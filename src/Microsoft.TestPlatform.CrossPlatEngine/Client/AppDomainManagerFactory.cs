// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
#if NET451
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;

    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common;

    /// <summary>
    /// Implementation for the Invoker which invokes engine in a new AppDomain
    /// Type of the engine must be a marshalable object for app domain calls and also must have a parameterless constructor
    /// </summary>
    internal class AppDomainExecutionManagerInvoker : IProxyExecutionManager
    {
        private const string XmlNamespace = "urn:schemas-microsoft-com:asm.v1";

        protected readonly AppDomain appDomain;

        private string mergedTempConfigFile = null;

        /// <summary>
        /// Serializer for the data objects
        /// </summary>
        private IDataSerializer dataSerializer;

        private NoIsolationInAppDomain proxyExecutionManagerInvoker;

        public bool IsInitialized => throw new NotImplementedException();

        public AppDomainExecutionManagerInvoker(string testSourcePath)
        {
            TestPlatformEventSource.Instance.TestHostAppDomainCreationStart();

            this.appDomain = CreateNewAppDomain(testSourcePath);
            this.dataSerializer = JsonDataSerializer.Instance;
            this.proxyExecutionManagerInvoker = this.CreateObject();

            TestPlatformEventSource.Instance.TestHostAppDomainCreationStop();
        }

        /// <summary>
        /// Create the object of type T in new AppDomain based on test source path
        /// </summary>
        /// <returns></returns>
        public NoIsolationInAppDomain CreateObject()
        {
            // Create Custom assembly resolver in new appdomain before anything else to resolve testplatform assemblies
            this.appDomain.CreateInstanceFromAndUnwrap(
                    typeof(CustomAssemblyResolver).Assembly.Location,
                    typeof(CustomAssemblyResolver).FullName,
                    false,
                    BindingFlags.Default,
                    null,
                    new object[] { Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) },
                    null,
                    null);

            // Create Invoker object in new appdomain
            var objectTypeType = typeof(NoIsolationInAppDomain);
            return (NoIsolationInAppDomain)appDomain.CreateInstanceFromAndUnwrap(
                    objectTypeType.Assembly.Location,
                    objectTypeType.FullName,
                    false,
                    BindingFlags.Default,
                    null,
                    null,
                    null,
                    null);
        }

        public void UnloadAppDomain()
        {
            try
            {
                if (appDomain != null)
                {
                    AppDomain.Unload(appDomain);
                }

                if (!string.IsNullOrWhiteSpace(this.mergedTempConfigFile) && File.Exists(mergedTempConfigFile))
                {
                    File.Delete(mergedTempConfigFile);
                }
            }
            catch
            {
                // ignore
            }
        }

        private AppDomain CreateNewAppDomain(string testSourcePath)
        {
            var appDomainSetup = new AppDomainSetup();
            var testSourceFolder = Path.GetDirectoryName(testSourcePath);

            // Set AppBase to TestAssembly location
            appDomainSetup.ApplicationBase = testSourceFolder;
            appDomainSetup.LoaderOptimization = LoaderOptimization.MultiDomainHost;

            // Set User Config file as app domain config
            SetConfigurationFile(appDomainSetup, testSourcePath, testSourceFolder);

            // Create new AppDomain
            return AppDomain.CreateDomain("InIsolationAppDomain", null, appDomainSetup);
        }

        private void SetConfigurationFile(AppDomainSetup appDomainSetup, string testSource, string testSourceFolder)
        {
            var userConfigFile = GetConfigFile(testSource, testSourceFolder);
            var hostAppConfigFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

            if (!string.IsNullOrEmpty(userConfigFile))
            {
                var userConfigDoc = XDocument.Load(userConfigFile);
                var testHostConfigDoc = XDocument.Load(hostAppConfigFile);

                // Merge user's config file and testHost config file and use merged one 
                var mergedConfigDocument = MergeApplicationConfigFiles(userConfigDoc, testHostConfigDoc);

                // Create a temp file with config
                this.mergedTempConfigFile = Path.GetTempFileName();
                mergedConfigDocument.Save(this.mergedTempConfigFile);

                // Set config file to merged one
                appDomainSetup.ConfigurationFile = this.mergedTempConfigFile;
            }
            else
            {
                // Use the current domains configuration setting.
                appDomainSetup.ConfigurationFile = hostAppConfigFile;
            }
        }

        private static string GetConfigFile(string testSource, string testSourceFolder)
        {
            string configFile = null;

            if (File.Exists(testSource + ".config"))
            {
                // Path to config file cannot be bad: storage is already checked, and extension is valid.
                configFile = testSource + ".config";
            }
            else
            {
                var netAppConfigFile = Path.Combine(testSourceFolder, "App.Config");
                if (File.Exists(netAppConfigFile))
                {
                    configFile = netAppConfigFile;
                }
            }

            return configFile;
        }

        protected static XDocument MergeApplicationConfigFiles(XDocument userConfigDoc, XDocument testHostConfigDoc)
        {
            // Start with User's config file as the base
            var mergedDoc = new XDocument(userConfigDoc);

            // Take testhost.exe Startup node 
            var startupNode = testHostConfigDoc.Descendants("startup")?.FirstOrDefault();
            if (startupNode != null)
            {
                // Remove user's startup and add ours which supports NET35 
                mergedDoc.Descendants("startup")?.Remove();
                mergedDoc.Root.Add(startupNode);
            }

            // Runtime node must be merged which contains assembly redirections
            var runtimeTestHostNode = testHostConfigDoc.Descendants("runtime")?.FirstOrDefault();
            if (runtimeTestHostNode != null)
            {
                var runTimeNode = mergedDoc.Descendants("runtime")?.FirstOrDefault();
                if (runTimeNode == null)
                {
                    // remove test host relative probing paths' element
                    // TestHost Probing Paths do not make sense since we are setting "AppBase" to user's test assembly location
                    runtimeTestHostNode.Descendants().Where((element) => string.Equals(element.Name.LocalName, "probing")).Remove();

                    // no runtime node exists in user's config - just add ours entirely
                    mergedDoc.Root.Add(runtimeTestHostNode);
                }
                else
                {
                    var assemblyBindingXName = XName.Get("assemblyBinding", XmlNamespace);
                    var mergedDocAssemblyBindingNode = mergedDoc.Descendants(assemblyBindingXName)?.FirstOrDefault();
                    var testHostAssemblyBindingNode = runtimeTestHostNode.Descendants(assemblyBindingXName)?.FirstOrDefault();

                    if (testHostAssemblyBindingNode != null)
                    {
                        if (mergedDocAssemblyBindingNode == null)
                        {
                            // add another assemblyBinding element as none exists in user's config
                            runTimeNode.Add(testHostAssemblyBindingNode);
                        }
                        else
                        {
                            var dependentAssemblyXName = XName.Get("dependentAssembly", XmlNamespace);
                            var redirections = testHostAssemblyBindingNode.Descendants(dependentAssemblyXName);

                            if (redirections != null)
                            {
                                mergedDocAssemblyBindingNode.Add(redirections);
                            }
                        }
                    }
                }
            }

            return mergedDoc;
        }

        public void Initialize()
        {
            var extensions = new List<string>();

            if (TestPluginCache.Instance.PathToExtensions != null)
            {
                extensions.AddRange(TestPluginCache.Instance.PathToExtensions.Where(ext => ext.EndsWith(TestPlatformConstants.TestAdapterEndsWithPattern, StringComparison.OrdinalIgnoreCase)));
            }

            extensions.AddRange(TestPluginCache.Instance.DefaultExtensionPaths);

            this.proxyExecutionManagerInvoker.Initialize(extensions);
        }

        public int StartTestRun(TestRunCriteria testRunCriteria, ITestRunEventsHandler eventHandler)
        {
            var rawMessage = this.dataSerializer.SerializePayload("faizan", testRunCriteria);
            this.proxyExecutionManagerInvoker.StartTestRun(rawMessage, eventHandler);
            return 0;
        }

        public void Cancel()
        {
            this.proxyExecutionManagerInvoker.Cancel();
        }

        public void Abort()
        {
            this.proxyExecutionManagerInvoker.Abort();
        }

        public void Close()
        {
        }
    }

    /// <summary>
    /// Custom Assembly resolver for child app domain to resolve testplatform assemblies
    /// </summary>
    internal class CustomAssemblyResolver : MarshalByRefObject
    {
        private readonly IDictionary<string, Assembly> resolvedAssemblies;

        private readonly string[] resolverPaths;

        public CustomAssemblyResolver(string testPlatformPath)
        {
            this.resolverPaths = new string[] { testPlatformPath, Path.Combine(testPlatformPath, "Extensions") };
            this.resolvedAssemblies = new Dictionary<string, Assembly>();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            Assembly assembly = null;
            lock (resolvedAssemblies)
            {
                try
                {
                    EqtTrace.Verbose("CurrentDomain_AssemblyResolve: Resolving assembly '{0}'.", args.Name);

                    if (resolvedAssemblies.TryGetValue(args.Name, out assembly))
                    {
                        return assembly;
                    }

                    // Put it in the resolved assembly so that if below Assembly.Load call
                    // triggers another assembly resolution, then we dont end up in stack overflow
                    resolvedAssemblies[args.Name] = null;

                    foreach (var path in resolverPaths)
                    {
                        var testPlatformFilePath = Path.Combine(path, assemblyName.Name) + ".dll";
                        if (File.Exists(testPlatformFilePath))
                        {
                            try
                            {
                                assembly = Assembly.LoadFrom(testPlatformFilePath);
                                break;
                            }
                            catch (Exception)
                            {
                                // ignore
                            }
                        }
                    }

                    // Replace the value with the loaded assembly
                    resolvedAssemblies[args.Name] = assembly;

                    return assembly;
                }
                finally
                {
                    if (null == assembly)
                    {
                        EqtTrace.Verbose("CurrentDomainAssemblyResolve: Failed to resolve assembly '{0}'.", args.Name);
                    }
                }
            }
        }
    }
#endif
}

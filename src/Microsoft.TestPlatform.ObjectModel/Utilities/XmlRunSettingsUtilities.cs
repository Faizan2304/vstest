// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using System.Xml.XPath;

    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using ObjectModelResources = Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources.Resources;

    /// <summary>
    /// Utilities for the run settings XML.
    /// </summary>
    public static class XmlRunSettingsUtilities
    {
        /// <summary>
        /// Gets the os architecture of the machine where this application is running
        /// </summary>
        public static ObjectModel.Architecture OSArchitecture
        {
            get
            {
                var arch = new PlatformEnvironment().Architecture;

                switch (arch)
                {
                    case PlatformArchitecture.X64:
                        return ObjectModel.Architecture.X64;
                    case PlatformArchitecture.X86:
                        return ObjectModel.Architecture.X86;
                    default:
                        return ObjectModel.Architecture.ARM;
                }
            }
        }

        /// <summary>
        /// Gets the settings to be used while creating XmlReader for runsettings.
        /// </summary>
        public static XmlReaderSettings ReaderSettings
        {
            get
            {
                return new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true, DtdProcessing = DtdProcessing.Prohibit };
            }
        }

        /// <summary>
        /// Removes the given settings node from run settings Xml
        /// </summary>
        /// <param name="settingsXml"></param>
        /// <param name="settingsName"></param>
        /// <returns></returns>
        public static string RemoveSettingsNode(string settingsXml, string settingsName)
        {
            ValidateArg.NotNullOrEmpty(settingsXml, "settingsXml");
            ValidateArg.NotNullOrEmpty(settingsName, "settingsName");

            XmlDocument doc = new XmlDocument();
            using (var xmlReader = XmlReader.Create(new StringReader(settingsXml), new XmlReaderSettings() { CloseInput = true }))
            {
                doc.Load(xmlReader);
            }
            XmlElement root = doc.DocumentElement;

            if (null != root[settingsName])
            {
                root.RemoveChild(root[settingsName]);
            }
            return doc.OuterXml;
        }

        /// <summary>
        /// Replaces (or adds) the given node in run settings Xml.
        /// </summary>
        /// <param name="settingsXml"></param>
        /// <param name="settingsNode"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202")]
        public static string ReplaceSettingsNode(string settingsXml, TestRunSettings settingsNode)
        {
            ValidateArg.NotNull(settingsNode, "settingsNode");
            ValidateArg.NotNull(settingsXml, "settingsXml");

            XmlElement newElement = settingsNode.ToXml();

            XmlDocument doc = new XmlDocument();

            using (var xmlReader = XmlReader.Create(new StringReader(settingsXml), new XmlReaderSettings() { CloseInput = true }))
            {
                doc.Load(xmlReader);
            }

            XmlElement root = doc.DocumentElement;


            if (null == root[settingsNode.Name])
            {
                XmlNode newNode = doc.ImportNode(newElement, true);
                root.AppendChild(newNode);
            }
            else
            {
                root[settingsNode.Name].InnerXml = newElement.InnerXml;
            }
            return doc.OuterXml;

        }

        /// <summary>
        /// Examines the given XPathNavigable representation of a runsettings file and determines if it has a configuration node
        /// for the data collector (used for Fakes and CodeCoverage)
        /// </summary>
        /// <param name="runSettingDocument"> XPathNavigable representation of a runsettings file </param>
        /// <param name="dataCollectorUri"> The data Collector Uri. </param>
        /// <returns> True if there is a datacollector configured. </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "1#")]
        public static bool ContainsDataCollector(IXPathNavigable runSettingDocument, string dataCollectorUri)
        {
            if (runSettingDocument == null)
            {
                throw new ArgumentNullException(nameof(runSettingDocument));
            }

            if (dataCollectorUri == null)
            {
                throw new ArgumentNullException(nameof(dataCollectorUri));
            }

            var navigator = runSettingDocument.CreateNavigator();
            var nodes = navigator.Select("/RunSettings/DataCollectionRunSettings/DataCollectors/DataCollector");

            foreach (XPathNavigator dataCollectorNavigator in nodes)
            {
                var uri = dataCollectorNavigator.GetAttribute("uri", string.Empty);
                if (string.Equals(dataCollectorUri, uri, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
#if NET451
        /// <summary>
        /// Returns a value that indicates if the Fakes data collector is already configured  in the settings.
        /// </summary>
        /// <param name="runSettings"></param>
        /// <returns></returns>

        public static bool ContainsFakesDataCollector(IXPathNavigable runSettings)
        {
            if (runSettings == null)
                throw new ArgumentNullException("runSettings");

            return XmlRunSettingsUtilities.ContainsDataCollector(runSettings, FakesMetadata.DataCollectorUri);
        }
#endif

        /// <summary>
        /// Adds the Fakes data collector settings in the run settings document.
        /// </summary>
        /// <param name="runSettings">A run settings document with a DataCollectors element available</param>
        /// <param name="fakesSettings"></param>
        public static DataCollectorSettings CreateFakesDataCollectorSettings()
        {
            // embed the fakes run settings
            var settings = new DataCollectorSettings
            {
                AssemblyQualifiedName = FakesMetadata.DataCollectorAssemblyQualifiedName,
                FriendlyName = FakesMetadata.FriendlyName,
                IsEnabled = true,
                Uri = new Uri(FakesMetadata.DataCollectorUri)
            };
            return settings;
        }

        /// <summary>
        /// Inserts a data collector settings in the file
        /// </summary>
        /// <param name="runSettingDocument">runSettingDocument</param>
        /// <param name="settings">settings</param>
        public static void InsertDataCollectorsNode(IXPathNavigable runSettingDocument, DataCollectorSettings settings)
        {
            if (runSettingDocument == null)
            {
                throw new ArgumentNullException(nameof(runSettingDocument));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var navigator = runSettingDocument.CreateNavigator();
            MoveToDataCollectorsNode(ref navigator);

            var settingsXml = settings.ToXml();
            var dataCollectorNode = settingsXml.CreateNavigator();
            dataCollectorNode.MoveToRoot();

            navigator.AppendChild(dataCollectorNode);
        }

        /// <summary>
        /// Returns RunConfiguration from settingsXml. 
        /// </summary>
        /// <param name="settingsXml">The run settings.</param>
        /// <returns> The RunConfiguration node as defined in the settings xml.</returns>
        public static RunConfiguration GetRunConfigurationNode(string settingsXml)
        {
            var nodeValue = GetNodeValue<RunConfiguration>(settingsXml, Constants.RunConfigurationSettingsName, RunConfiguration.FromXml);
            if (nodeValue == default(RunConfiguration))
            {
                // Return default one.
                nodeValue = new RunConfiguration();
            }

            return nodeValue;
        }

        /// <summary>
        /// Gets the set of user defined test run parameters from settings xml as key value pairs.
        /// </summary>
        /// <param name="settingsXml">The run settings xml.</param>
        /// <returns>The test run parameters defined in the run settings.</returns>
        /// <remarks>If there is no test run parameters section defined in the settings xml a blank dictionary is returned.</remarks>
        public static Dictionary<string, object> GetTestRunParameters(string settingsXml)
        {
            var nodeValue = GetNodeValue<Dictionary<string, object>>(settingsXml, Constants.TestRunParametersName, TestRunParameters.FromXml);
            if (nodeValue == default(Dictionary<string, object>))
            {
                // Return default.
                nodeValue = new Dictionary<string, object>();
            }

            return nodeValue;
        }

        /// <summary>
        /// Create a default run settings
        /// </summary>
        /// <returns>
        /// The <see cref="IXPathNavigable"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlReaderSettings.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        public static IXPathNavigable CreateDefaultRunSettings()
        {
            // Create a new default xml doc that looks like this:
            // <?xml version="1.0" encoding="utf-8"?>
            // <RunSettings>
            //   <DataCollectionRunSettings>
            //     <DataCollectors>
            //     </DataCollectors>
            //   </DataCollectionRunSettings>
            // </RunSettings>
            var doc = new XmlDocument();
            var xmlDeclaration = doc.CreateNode(XmlNodeType.XmlDeclaration, string.Empty, string.Empty);

            doc.AppendChild(xmlDeclaration);
            var runSettingsNode = doc.CreateElement(Constants.RunSettingsName);
            doc.AppendChild(runSettingsNode);

            var dataCollectionRunSettingsNode = doc.CreateElement(Constants.DataCollectionRunSettingsName);
            runSettingsNode.AppendChild(dataCollectionRunSettingsNode);

            var dataCollectorsNode = doc.CreateElement(Constants.DataCollectorsSettingName);
            dataCollectionRunSettingsNode.AppendChild(dataCollectorsNode);

#if NET451
            return doc;
#else
            return doc.ToXPathNavigable();
#endif
        }

        /// <summary>
        /// Returns whether data collection is enabled in the parameter settings xml or not
        /// </summary>
        /// <param name="runSettingsXml"> The run Settings Xml. </param>
        /// <returns> True if data collection is enabled. </returns>
        public static bool IsDataCollectionEnabled(string runSettingsXml)
        {
            var dataCollectionRunSettings = GetDataCollectionRunSettings(runSettingsXml);

            if (dataCollectionRunSettings == null || !dataCollectionRunSettings.IsCollectionEnabled)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns whether in proc data collection is enabled in the parameter settings xml or not
        /// </summary>
        /// <param name="runSettingsXml"> The run Settings Xml. </param>
        /// <returns> True if data collection is enabled. </returns>
        public static bool IsInProcDataCollectionEnabled(string runSettingsXml)
        {
            var dataCollectionRunSettings = GetInProcDataCollectionRunSettings(runSettingsXml);

            if (dataCollectionRunSettings == null || !dataCollectionRunSettings.IsCollectionEnabled)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get DataCollection Run settings from the settings XML.
        /// </summary>
        /// <param name="runSettingsXml"> The run Settings Xml. </param>
        /// <returns> The <see cref="DataCollectionRunSettings"/>. </returns>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlReaderSettings.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        public static DataCollectionRunSettings GetDataCollectionRunSettings(string runSettingsXml)
        {
            // use XmlReader to avoid loading of the plugins in client code (mainly from VS).
            if (string.IsNullOrWhiteSpace(runSettingsXml))
            {
                return null;
            }

            using (var stringReader = new StringReader(runSettingsXml))
            {
                var reader = XmlReader.Create(stringReader, ReaderSettings);

                // read to the fist child
                XmlReaderUtilities.ReadToRootNode(reader);
                reader.ReadToNextElement();

                // Read till we reach DC element or reach EOF
                while (!string.Equals(reader.Name, Constants.DataCollectionRunSettingsName)
                       &&
                       !reader.EOF)
                {
                    reader.SkipToNextElement();
                }

                // If reached EOF => DC element not there
                if (reader.EOF)
                {
                    return null;
                }

                // Reached here => DC element present. 
                return DataCollectionRunSettings.FromXml(reader);
            }
        }

        /// <summary>
        /// Get InProc DataCollection Run settings
        /// </summary>
        /// <param name="runSettingsXml">
        /// The run Settings Xml.
        /// </param>
        /// <returns>Data collection run settings.</returns>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlReaderSettings.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        public static DataCollectionRunSettings GetInProcDataCollectionRunSettings(string runSettingsXml)
        {
            // use XmlReader to avoid loading of the plugins in client code (mainly from VS).
            if (!string.IsNullOrWhiteSpace(runSettingsXml))
            {
                runSettingsXml = runSettingsXml.Trim();
                using (StringReader stringReader1 = new StringReader(runSettingsXml))
                {
                    XmlReader reader = XmlReader.Create(stringReader1, ReaderSettings);

                    // read to the fist child
                    XmlReaderUtilities.ReadToRootNode(reader);
                    reader.ReadToNextElement();

                    // Read till we reach In Proc IDC element or reach EOF
                    while (!string.Equals(reader.Name, Constants.InProcDataCollectionRunSettingsName)
                           &&
                           !reader.EOF)
                    {
                        reader.SkipToNextElement();
                    }

                    // If reached EOF => IDC element not there
                    if (reader.EOF)
                    {
                        return null;
                    }

                    // Reached here => In Proc IDC element present.
                    return DataCollectionRunSettings.FromXml(reader, Constants.InProcDataCollectionRunSettingsName, Constants.InProcDataCollectorsSettingName, Constants.InProcDataCollectorSettingName);
                }
            }

            return null;
        }

        /// <summary>
        /// Throws a settings exception if the node the reader is on has attributes defined.
        /// </summary>
        /// <param name="reader"> The xml reader. </param>
        internal static void ThrowOnHasAttributes(XmlReader reader)
        {
            if (reader.HasAttributes)
            {
                reader.MoveToNextAttribute();
                throw new SettingsException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Resources.InvalidSettingsXmlAttribute,
                        Constants.RunConfigurationSettingsName,
                        reader.Name));
            }
        }

        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlReaderSettings.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        private static T GetNodeValue<T>(string settingsXml, string nodeName, Func<XmlReader, T> nodeParser)
        {
            // use XmlReader to avoid loading of the plugins in client code (mainly from VS).
            if (!string.IsNullOrWhiteSpace(settingsXml))
            {
                using (var stringReader = new StringReader(settingsXml))
                {
                    XmlReader reader = XmlReader.Create(stringReader, ReaderSettings);

                    // read to the fist child
                    XmlReaderUtilities.ReadToRootNode(reader);
                    reader.ReadToNextElement();

                    // Read till we reach nodeName element or reach EOF
                    while (!string.Equals(reader.Name, nodeName, StringComparison.OrdinalIgnoreCase)
                            &&
                            !reader.EOF)
                    {
                        reader.SkipToNextElement();
                    }

                    if (!reader.EOF)
                    {
                        // read nodeName element.
                        return nodeParser(reader);
                    }
                }
            }

            return default(T);
        }

        /// <summary>
        /// Moves the given runsettings file navigator to the DataCollectors node in the runsettings xml.
        /// Throws XmlException if it was unable to find the DataCollectors node.
        /// </summary>
        /// <param name="runSettingsNavigator">XPathNavigator for a runsettings xml document.</param>
        private static void MoveToDataCollectorsNode(ref XPathNavigator runSettingsNavigator)
        {
            runSettingsNavigator.MoveToRoot();
            if (!runSettingsNavigator.MoveToChild("RunSettings", string.Empty))
            {
                throw new XmlException(string.Format(CultureInfo.CurrentCulture, ObjectModelResources.CouldNotFindXmlNode, "RunSettings"));
            }

            if (!runSettingsNavigator.MoveToChild("DataCollectionRunSettings", string.Empty))
            {
                runSettingsNavigator.AppendChildElement(string.Empty, "DataCollectionRunSettings", string.Empty, string.Empty);
                runSettingsNavigator.MoveToChild("DataCollectionRunSettings", string.Empty);
            }

            if (!runSettingsNavigator.MoveToChild("DataCollectors", string.Empty))
            {
                runSettingsNavigator.AppendChildElement(string.Empty, "DataCollectors", string.Empty, string.Empty);
                runSettingsNavigator.MoveToChild("DataCollectors", string.Empty);
            }
        }

        static class FakesMetadata
        {
            /// <summary>
            /// Friendly name of the data collector
            /// </summary>
            public const string FriendlyName = "UnitTestIsolation";
            /// <summary>
            /// Gets the URI of the data collector
            /// </summary>
            public const string DataCollectorUri = "datacollector://microsoft/unittestisolation/1.0";
            /// <summary>
            /// Gets the assembly qualified name of the data collector type
            /// </summary>
            public const string DataCollectorAssemblyQualifiedName = "Microsoft.VisualStudio.TraceCollector.UnitTestIsolationDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
        }
    }
}

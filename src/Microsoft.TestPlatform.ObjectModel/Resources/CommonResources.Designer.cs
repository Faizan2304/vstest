﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel {
    using System;
    using System.Reflection;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class CommonResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CommonResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.VisualStudio.TestPlatform.ObjectModel.CommonResources", typeof(CommonResources).GetTypeInfo().Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The parameter cannot be null or empty..
        /// </summary>
        public static string CannotBeNullOrEmpty {
            get {
                return ResourceManager.GetString("CannotBeNullOrEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Test run will use DLL(s) built for framework {0} and platform {1}. Following DLL(s) will not be part of run: {2} Go to {3} for more details on managing these settings..
        /// </summary>
        public static string DisplayChosenSettings {
            get {
                return ResourceManager.GetString("DisplayChosenSettings", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Could not find file {0}..
        /// </summary>
        public static string FileNotFound
        {
            get
            {
                return ResourceManager.GetString("FileNotFound", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to None of the provided test containers match the Platform Architecture and .Net Framework settings for the test run. Platform: {0}  .Net Framework: {1}. Go to http://go.microsoft.com/fwlink/?LinkID=330428 for more details on managing these settings..
        /// </summary>
        public static string NoMatchingSourcesFound {
            get {
                return ResourceManager.GetString("NoMatchingSourcesFound", resourceCulture);
            }
        }
    }
}

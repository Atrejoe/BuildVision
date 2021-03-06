using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using AlekseyNagovitsyn.BuildVision.Core.Logging;

using EnvDTE;
using EnvDTE80;

using Microsoft.Win32;
using VSLangProj;

namespace AlekseyNagovitsyn.BuildVision.Helpers
{
    public static class ProjectExtensions
    {
        #region ProjectTypes fields

        private static readonly Dictionary<string, string> _knownProjectTypes = new Dictionary<string, string>
            {
                {"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "Windows"}, // C#
                {"{F184B08F-C81C-45F6-A57F-5ABD9991F28F}", "Windows"}, // VB.NET
                {"{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}", "Windows"}, // C++
                {"{F2A71F9B-5D33-465A-A702-920D77279786}", "Windows"}, // F#
                {"{349C5851-65DF-11DA-9384-00065B846F21}", "Web Application"},
                {"{E24C65DC-7377-472B-9ABA-BC803B73C61A}", "Web Site"},
                {"{603c0e0b-db56-11dc-be95-000d561079b0}", "ASP.NET MVC1"},
                {"{F85E285D-A4E0-4152-9332-AB1D724D3325}", "ASP.NET MVC2"},
                {"{E53F8FEA-EAE0-44A6-8774-FFD645390401}", "ASP.NET MVC3"},
                {"{E3E379DF-F4C6-4180-9B81-6769533ABE47}", "ASP.NET MVC4"},
                {"{F135691A-BF7E-435D-8960-F99683D2D49C}", "Distributed System"},
                {"{3D9AD99F-2412-4246-B90B-4EAA41C64699}", "WCF"},
                {"{60DC8134-EBA5-43B8-BCC9-BB4BC16C2548}", "WPF"},
                {"{C252FEB5-A946-4202-B1D4-9916A0590387}", "Visual DB Tools"},
                {"{A9ACE9BB-CECE-4E62-9AA4-C7E7C5BD2124}", "Database"},
                {"{4F174C21-8C12-11D0-8340-0000F80270F8}", "Database"},
                {"{3AC096D0-A1C2-E12C-1390-A8335801FDAB}", "Test"},
                {"{20D4826A-C6FA-45DB-90F4-C717570B9F32}", "Legacy (2003) Smart Device"},
                {"{CB4CE8C6-1BDB-4DC7-A4D3-65A1999772F8}", "Legacy (2003) Smart Device"},
                {"{4D628B5B-2FBC-4AA6-8C16-197242AEB884}", "Smart Device"},
                {"{68B1623D-7FB9-47D8-8664-7ECEA3297D4F}", "Smart Device"},
                {"{14822709-B5A1-4724-98CA-57A101D1B079}", "Workflow"},
                {"{D59BE175-2ED0-4C54-BE3D-CDAA9F3214C8}", "Workflow"},
                {"{06A35CCD-C46D-44D5-987B-CF40FF872267}", "Deployment Merge Module"},
                {"{3EA9E505-35AC-4774-B492-AD1749C4943A}", "Deployment Cab"},
                {"{978C614F-708E-4E1A-B201-565925725DBA}", "Deployment Setup"},
                {"{AB322303-2255-48EF-A496-5904EB18DA55}", "Deployment Smart Device Cab"},
                {"{A860303F-1F3F-4691-B57E-529FC101A107}", "VSTA"},
                {"{BAA0C2D2-18E2-41B9-852F-F413020CAA33}", "VSTO"},
                {"{F8810EC1-6754-47FC-A15F-DFABD2E3FA90}", "SharePoint Workflow"},
                {"{6D335F3A-9D43-41b4-9D22-F6F17C4BE596}", "XNA (Windows)"},
                {"{2DF5C3F4-5A5F-47a9-8E94-23B4456F55E2}", "XNA (XBox)"},
                {"{D399B71A-8929-442a-A9AC-8BEC78BB2433}", "XNA (Zune)"},
                {"{EC05E597-79D4-47f3-ADA0-324C4F7C7484}", "SharePoint"},
                {"{593B0543-81F6-4436-BA1E-4747859CAAE2}", "SharePoint"},
                {"{A1591282-1198-4647-A2B1-27E5FF5F6F3B}", "Silverlight"},
                {"{F088123C-0E9E-452A-89E6-6BA2F21D5CAC}", "Modeling"},
                {"{82B43B9B-A64C-4715-B499-D71E9CA2BD60}", "Extensibility"}
            };

        private const string UnknownProjectTypeLabel = "Unknown";

        #endregion

        #region Languages fields

        private static readonly Dictionary<string, string> _knownLanguages = new Dictionary<string, string>
            {
                { CodeModelLanguageConstants.vsCMLanguageCSharp, "C#" },
                { CodeModelLanguageConstants.vsCMLanguageIDL, "IDL" },
                { CodeModelLanguageConstants.vsCMLanguageMC, "MC++" }, // Managed C++
                { CodeModelLanguageConstants.vsCMLanguageVB, "VB.NET" },
                { CodeModelLanguageConstants.vsCMLanguageVC, "VC++" }, // Visual C++
                { CodeModelLanguageConstants2.vsCMLanguageJSharp, "J#" },
                { "{F2A71F9B-5D33-465A-A702-920D77279786}", "F#" },
            };

        private const string UnknownLanguageLabel = "Unknown";

        #endregion

        public static string GetLanguageName(this Project project)
        {
            try
            {
                if (project.CodeModel == null)
                {
                    var kind = project.Kind.ToUpper();
                    return _knownLanguages.ContainsKey(kind) ? _knownLanguages[kind] : Resources.GridCellNoneText;
                }

                string codeModelLanguageKind = project.CodeModel.Language;
                return _knownLanguages.ContainsKey(codeModelLanguageKind) ? _knownLanguages[codeModelLanguageKind] : UnknownLanguageLabel;
            }
            catch (NotImplementedException /*throws on project.CodeModel getter*/)
            {
                return Resources.GridCellNoneText;
            }
            catch (Exception ex)
            {
                ex.TraceUnknownException();
                return Resources.GridCellNAText;
            }
        }

        public static IEnumerable<string> GetFlavourTypes(this Project project)
        {
            try
            {
                var flavourTypes = new List<string>();

                string guidsStr = project.GetProjectTypeGuids();
                if (!string.IsNullOrWhiteSpace(guidsStr))
                {
                    string[] typeGuids = guidsStr.Trim(';').Split(';');
                    flavourTypes.AddRange(typeGuids.Select(prjKind => GetProjectType(prjKind, project.DTE.Version)));
                }

                var myType = project.TryGetPropertyValueOrDefault("MyType"); // for VB.NET projects
                if (myType != null && myType.ToString() != "Empty")
                    flavourTypes.Add(myType.ToString());

                var keyword = project.TryGetPropertyValueOrDefault("keyword"); // for C++ projects
                if (keyword != null)
                    flavourTypes.Add(keyword.ToString());

                var filteredValues = flavourTypes.Where(str => !string.IsNullOrWhiteSpace(str)).Distinct();
                return filteredValues;
            }
            catch (Exception ex)
            {
                ex.TraceUnknownException();
                return Enumerable.Empty<string>();
            }
        }

        public static string GetOutputType(this Project project)
        {
            try
            {
                string prjObjTypeName = project.Object.GetType().Name;
                if (prjObjTypeName == "VCProjectShim" || prjObjTypeName == "VCProject")
                {
                    dynamic prj = project.Object;
                    dynamic configs = prj.Configurations;
                    dynamic config = configs.Item(project.ConfigurationManager.ActiveConfiguration.ConfigurationName);
                    string configType = config.ConfigurationType.ToString();

                    switch (configType)
                    {
                        case "typeUnknown":
                            return "Unknown";
                        case "typeApplication":
                            return "WinExe";
                        case "typeDynamicLibrary":
                            return "Library (dynamic)";
                        case "typeStaticLibrary":
                            return "Library (static)";
                        case "typeGeneric":
                            return "Makefile";
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    var property = project.GetPropertyOrDefault("OutputType");
                    if (property == null || property.Value == null)
                        return Resources.GridCellNoneText;

                    var outputType = (prjOutputType)property.Value;
                    switch (outputType)
                    {
                        case prjOutputType.prjOutputTypeWinExe:
                            return "WinExe";
                        case prjOutputType.prjOutputTypeExe:
                            return "WinExe (console)";
                        case prjOutputType.prjOutputTypeLibrary:
                            return "Library";
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.TraceUnknownException();
                return Resources.GridCellNAText;
            }
        }

        public static string GetExtenderNames(this Project project)
        {
            try
            {
                var extenderNames = (object[])project.ExtenderNames;
                if (extenderNames == null || extenderNames.Length == 0)
                    return Resources.GridCellNoneText;

                return string.Join("; ", extenderNames);
            }
            catch (Exception ex)
            {
                ex.TraceUnknownException();
                return Resources.GridCellNAText;
            }
        }

        #region GetProjectType

        /// <summary>
        /// Get user-friendly type of the project.
        /// </summary>
        /// <param name="projectKind">The <see cref="Project.Kind"/>.</param>
        /// <param name="version">The <see cref="_DTE.Version"/>. For Visual Studio 2012 is "12.0".</param>
        /// <returns>User-friendly type of the project.</returns>
        public static string GetProjectType(string projectKind, string version)
        {
            return GetKnownProjectType(projectKind) ?? GetProjectTypeFromRegistry(projectKind, version) ?? UnknownProjectTypeLabel;
        }

        private static string GetKnownProjectType(string projectKind)
        {
            projectKind = projectKind.ToUpper();
            return _knownProjectTypes.ContainsKey(projectKind) ? _knownProjectTypes[projectKind] : null;
        }

        private static string GetProjectTypeFromRegistry(string projectKind, string version)
        {
            try
            {
                string key = string.Format(@"SOFTWARE\Microsoft\VisualStudio\{0}\Projects\{1}", version, projectKind);
                var subKey = Registry.LocalMachine.OpenSubKey(key);
                var type = (subKey != null) ? subKey.GetValue(string.Empty, string.Empty).ToString() : string.Empty;
                if (type.StartsWith("#"))
                {
                    Debug.Assert(subKey != null, "subKey != null");
                    type = string.Format("{0} project", subKey.GetValue("Language(VsTemplate)", string.Empty));
                }

                // Types usually have "Project Factory" and "ProjectFactory" postfixes: Web MVC Project Factory, ExtensibilityProjectFactory.
                var typeProjectFactoryPostfixes = new[] { " Project Factory", "ProjectFactory" };
                foreach (var postfix in typeProjectFactoryPostfixes)
                {
                    if (type.EndsWith(postfix))
                    {
                        type = type.Remove(type.LastIndexOf(postfix, StringComparison.InvariantCulture));
                        break;
                    }
                }

                TraceManager.Trace(
                    string.Format("Project type is taken from the registry: Kind={0}, DTEVersion={1}, Type={2}", projectKind, version, type), 
                    EventLogEntryType.Warning);

                if (string.IsNullOrWhiteSpace(type))
                    return null;

                _knownProjectTypes[projectKind] = type;
                return type;
            }
            catch (Exception ex)
            {
                ex.Trace(string.Format("Unable to get project type from registry: (Kind={0}, DTEVersion={1})", projectKind, version));
                return null;
            }
        }

        #endregion

        public static string GetRootNamespace(this Project project)
        {
            var val = project.TryGetPropertyValueOrDefault("RootNamespace") ?? project.TryGetPropertyValueOrDefault("DefaultNamespace");
            return (val != null) ? val.ToString() : null;
        }

        /// <summary>
        /// Gets the project or at least one of his children is dirty.
        /// </summary>
        public static bool IsDirty(this Project project)
        {
            if (project.IsDirty)
                return true;

            if (project.ProjectItems != null && project.ProjectItems.Cast<ProjectItem>().Any(ProjectItemIsDirty))
                return true;

            return false;
        }

        private static bool ProjectItemIsDirty(this ProjectItem projectItem)
        {
            if (projectItem.IsDirty)
                return true;

            if (projectItem.ProjectItems != null && projectItem.ProjectItems.Cast<ProjectItem>().Any(ProjectItemIsDirty))
                return true;

            return false;
        }
    }
}
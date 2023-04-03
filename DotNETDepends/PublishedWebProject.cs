﻿using Microsoft.CodeAnalysis;
using System.Xml;
using Disassembler;
using System.Transactions;

namespace DotNETDepends
{
    /**
     * This class represents an ASP.NET Core or Blazor web project that has
     * had it's solution published.
     */
    internal class PublishedWebProject : RoslynProject
    {
        private readonly string runtime;
        private readonly string config;

        private string? assemblyName;
        private string? rootNamespace;
        private string? sdk;
        public string? SDK { get { return sdk; } }

        public PublishedWebProject(Project project, string runtime, string config, Dependencies dependencies, IErrorReporter errorReporter) : base(project, dependencies, errorReporter)
        {
            this.runtime = runtime;
            this.config = config;
            ReadProjectFile(errorReporter);
        }

        private static void AddFilesToList(List<string> paths, FileInfo[] files)
        {
            foreach (var file in files)
            {
                paths.Add(file.FullName);
            }
        }

        /*
         * Get all web files
         */
        private List<string> GetSourceFiles()
        {
            var result = new List<string>();
            var dir = Path.GetDirectoryName(project.FilePath);
            if (dir != null)
            {
                var dirInfo = new DirectoryInfo(dir);
                AddFilesToList(result, dirInfo.GetFiles("*.cshtml", SearchOption.AllDirectories));
                AddFilesToList(result, dirInfo.GetFiles("*.vbhtml", SearchOption.AllDirectories));
                AddFilesToList(result, dirInfo.GetFiles("*.razor", SearchOption.AllDirectories));
            }
            return result;
        }

        /*
         * Reads the project file (an xml document) and extracts the Sdk, AssemblyName and 
         * RootNamespace.  We need all 3 to map assembly classes to web source files
         */
        private void ReadProjectFile(IErrorReporter errorReporter)
        {
            if (project.FilePath != null)
            {
                var xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.Load(project.FilePath);
                    if (xmlDoc.DocumentElement != null)
                    {
                        sdk = xmlDoc.DocumentElement.GetAttribute("Sdk");

                        switch (sdk)
                        {
                            case SupportedSdks.ASP_NET_CORE:
                            case SupportedSdks.BLAZOR_WASM:
                                var assemblyNodes = xmlDoc.GetElementsByTagName("AssemblyName");
                                if (assemblyNodes.Count > 0)
                                {
                                    assemblyName = assemblyNodes.Item(0)?.InnerText;
                                }
                                else
                                {
                                    assemblyName = Path.GetFileNameWithoutExtension(project.FilePath) + ".dll";
                                }
                                var rootNSNodes = xmlDoc.GetElementsByTagName("RootNamespace");
                                if (rootNSNodes.Count > 0)
                                {
                                    rootNamespace = rootNSNodes.Item(0)?.InnerText;
                                }
                                else
                                {
                                    rootNamespace = Path.GetFileNameWithoutExtension(project.FilePath);
                                }
                                break;
                        }

                    }

                }
                catch (Exception ex)
                {
                    errorReporter.AddErrorMessage(ex.ToString());
                }
            }
        }

        /*
         * Locates the compiled assembly in the output folder (bin)
         */

        private string? FindAssembly()
        {
            if (project.FilePath != null && assemblyName != null)
            {
                var binRoot = Path.Combine(new string[] { Path.GetDirectoryName(project.FilePath)?? "", "bin", config });
                if (Path.Exists(binRoot))
                {
                    DirectoryInfo dirInfo = new(binRoot);
                    var dirs = dirInfo.GetDirectories();
                    string? assemPath = null;
                    if (dirs.Length == 1)
                    {
                        assemPath = Path.Combine(new string[] { binRoot, dirs[0].Name, runtime, "publish", assemblyName });
                        if (File.Exists(assemPath))
                        {
                            return assemPath;
                        }
                        assemPath = Path.Combine(new string[] { binRoot, dirs[0].Name, runtime, assemblyName });
                        if (File.Exists(assemPath))
                        {
                            return assemPath;
                        }
                        assemPath = Path.Combine(new string[] { binRoot, dirs[0].Name, assemblyName });
                    }
                    if (assemPath != null && File.Exists(assemPath))
                    {
                        return assemPath;
                    }
                }
            }
            return null;
        }

        public bool IsSupportedSDK()
        {
            if (sdk == null)
            {
                return false;
            }
            return SupportedSdks.IsSupportedSDK(sdk);
        }

        /*
         * Performs the following in order:
         * 1.  Find the assembly.
         * 2.  Map source files to namespaces and types based on the Sdk of the project
         * 3.  Disassembles the types from the located assembly for type definition and
         *     references.
         */
        public async Task<bool> Analyze(HashSet<SourceType> foundTypes, IErrorReporter errorReporter)
        {
            var assemblyPath = FindAssembly();
            string? solutionDir = Path.GetDirectoryName(project.Solution.FilePath);
            if (assemblyPath != null && sdk != null && solutionDir != null)
            {

                var builder = SourceTypeBuilderFactory.Create(sdk);
                var projectDir = Path.GetDirectoryName(project.FilePath);
                if (builder != null && projectDir != null)
                {
                    var sources = GetSourceFiles();
                    SourceTypeLocator locator = new(builder);
                    foreach (var source in sources)
                    {
                        locator.AddSource(source, projectDir, solutionDir, rootNamespace ?? "");
                    }
                    var reader = new AssemblyReader(assemblyPath, locator, errorReporter);
                    reader.Read();
                    await base.Analyze();
                    foundTypes.UnionWith(reader.GetReadTypes());
                    return true;
                }

            }
            errorReporter.AddErrorMessage("Unable to locate assembly for project: " + project.Name);
            //Go ahead and do the C# and VB files
            await base.Analyze();
            return false;
        }

    }
}

using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Disassembler;
using DotNETDepends.Output;

namespace DotNETDepends
{
    /**
     * General overview:
     * This class takes a solution and processes each project in it in dependency order.
     * If it encounters an ASP.NET project, it will call "dotnet publish" on it.
     * That compiles all of the ASP.NET content.  We then map from source to generated class
     * and disassemble those page classes looking for declarations and referenced symbols.
     * The C# and VB files are parsed with Roslyn to look for declared symbols only.  We then
     * find any references to those symbol both in the solution and in the disassembled ASP.NET
     * classes.
     */
    public class SolutionReader
    {
        private const string PUBLISH_CONFIG = "Release";
        private bool isPublished = false;
        private readonly Dependencies dependencies = new();

        public SolutionReader()
        {

        }

        /**
         * Reads the solution and process the projects in dependency order
         */
        public async Task ReadSolutionAsync(String path, AnalysisOutput output)
        {
            
            var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(path).ConfigureAwait(false);
            
            RestoreSolution(solution, output);

            var depGraph = solution.GetProjectDependencyGraph();
            var dependencyOrderedProjects = depGraph.GetTopologicallySortedProjects();
            //Read all of the projects declared types and references (if ASP.NET)
            foreach (var projectId in dependencyOrderedProjects)
            {
                await ProcessProjectAsync(projectId, solution, depGraph, output).ConfigureAwait(false);
            }
            await ResolveDepenedenciesAsync(solution).ConfigureAwait(false);
            dependencies.GetLinks(output);
        }

        /**
         * Processes all of the declared and referenced symbols we collected
         */
        private async Task ResolveDepenedenciesAsync(Solution solution)
        {
            var solutionRoot = Path.GetDirectoryName(solution.FilePath);
            if (solutionRoot != null)
            {
                /**
                 * These entries come from Roslyn.  They are the declared types in
                 * each file we walked.
                 */
                foreach (var entry in dependencies.GetFileEntries())
                {

                    foreach (var symbol in entry.Symbols)
                    {
                        /**
                         * For every symbol declaration we found, find all references in the solution to that symbol.
                         * This will only cover C# abd VB source files.
                         */
                        var references = await SymbolFinder.FindReferencesAsync(symbol, solution).ConfigureAwait(false);

                        foreach (var reference in references)
                        {
                            foreach (var location in reference.Locations)
                            {
                                //CandidateLocations are guesses by Roslyn.
                                //Also filter anything we don't have source for
                                if (location.Location.IsInSource)
                                {
                                    //Record the reference
                                    var path = location.Location.SourceTree.FilePath;
                                    var sourceEntry = dependencies.GetEntry(Path.GetRelativePath(solutionRoot, path));
                                    sourceEntry?.AddDependency(entry.FilePath);
                                }
                            }
                        }
                        /**
                         * Now look for references in the ASP.NET file we disassembled.
                         */
                        var consumers = dependencies.FindSourceSymbolReferences(symbol);
                        foreach (var consumer in consumers)
                        {
                            consumer.Dependencies.Add(entry.FilePath);
                        }

                    }
                }
                /**
                 * Find dependencies within all of the decompiled source types
                 * to each other.
                 * These are unlikely, but possible with Blazor components
                 */
                foreach (var srcType in dependencies.SourceTypes)
                {
                    var deps = dependencies.FindSourceSymbolReferences(srcType);
                    foreach (var dep in deps)
                    {
                        dep.Dependencies.Add(srcType.Path);
                    }
                }
            }
        }

        /**
         * Runs dotnet restore on the solution.  This fetches any nuget dependencies
         * so that when we compile with Roslyn or decompile the assembly they are available.
         */
        private bool RestoreSolution(Solution solution, AnalysisOutput analysisOutput)
        {
            var dotnetPath = SDKTools.GetDotnetPath();
            if (solution.FilePath != null)
            {
                try
                {
                    var startInfo = new ProcessStartInfo(dotnetPath)
                    {
                        WorkingDirectory = Path.GetDirectoryName(solution.FilePath)
                    };
                    //Executes:
                    //dotnet restore <solutionfile>
                    startInfo.ArgumentList.Add("restore");
                    //This is needed for windows projects on linux
                    startInfo.ArgumentList.Add("/p:EnableWindowsTargeting=true");
                    startInfo.ArgumentList.Add(solution.FilePath);

                    var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        process.WaitForExit();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    analysisOutput.AddErrorMessage("Exception: " + ex.ToString());
                }
            }
            return false;
        }

        /**
         * When we find ASP.NET projects, publish the solution with "dotnet publish".
         * This will compile everything and put all of the dependencies in to the output folder
         * so that we can resolve everything when we disassemble the ASP.NET content.
         */
        private bool PublishSolution(Solution solution, AnalysisOutput analysisOutput)
        {
            //Only do this once
            if (isPublished)
            {
                return true;
            }
            //even if the publish fails, mark it published so we don't retry
            isPublished = true;
            var dotnetPath = SDKTools.GetDotnetPath();
            if (solution.FilePath != null)
            {
                try
                {
                    var startInfo = new ProcessStartInfo(dotnetPath)
                    {
                        WorkingDirectory = Path.GetDirectoryName(solution.FilePath)
                    };
                    //Executes:
                    //dotnet publish -c Release -r win-x64 -p:PublishReadyToRun=true <solutionfile>
                    startInfo.ArgumentList.Add("publish");
                    startInfo.ArgumentList.Add("-c");
                    startInfo.ArgumentList.Add(PUBLISH_CONFIG);
                    startInfo.ArgumentList.Add("-r");
                    startInfo.ArgumentList.Add(RuntimeInformation.RuntimeIdentifier);
                    //PublishReadyToRun is required to precompile the ASP and Blazor files
                    startInfo.ArgumentList.Add("-p:PublishReadyToRun=true");
                    startInfo.ArgumentList.Add("--self-contained");
                    startInfo.ArgumentList.Add(solution.FilePath);
                    var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        process.WaitForExit();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    analysisOutput.AddErrorMessage("Exception: " + ex.ToString());
                }
            }
            return false;
        }

        /**
         * Check the project for razor, cshtml or vbhtml files.
         */
        private static bool ProjectContainsNETWebFiles(Project project)
        {
            if (project.FilePath != null)
            {
                var dirInfo = new DirectoryInfo(Path.GetDirectoryName(project.FilePath));
                var cshtml = dirInfo.GetFiles("*.cshtml", SearchOption.AllDirectories);
                if (cshtml.Length > 0)
                {
                    return true;
                }
                var vbhtml = dirInfo.GetFiles("*.vbhtml", SearchOption.AllDirectories);
                if (vbhtml.Length > 0)
                {
                    return true;
                }
                var razor = dirInfo.GetFiles("*.razor", SearchOption.AllDirectories);
                if (razor.Length > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * Process a project for it's dependencies
         * It first build the project dependency graph.  It then checks for ASP.NET files, 
         * and if found publishes the solution if it hasn't been.
         * If the project is contains web files it uses the published assemblies to reflect the
         * dependencies and disassembles the generated ASP/Razor pages/components.
         * It then uses Roslyn to parse the references of any C# or VB files, regardless of
         * whether or not it is an ASP.NET project.
         */
        private async Task ProcessProjectAsync(ProjectId projectId, Solution solution, ProjectDependencyGraph depGraph, AnalysisOutput analysisOutput)
        {

            var project = solution.GetProject(projectId);

            var solutionRoot = Path.GetDirectoryName(solution.FilePath);
            if (project != null && project.FilePath != null && solutionRoot != null)
            {
                var depEntry = dependencies.CreateProjectEntry(Path.GetRelativePath(solutionRoot, project.FilePath));

                var projectDependencies = depGraph.GetProjectsThatThisProjectDirectlyDependsOn(projectId);
                //Add all dependent projects to the entry
                foreach (var depId in projectDependencies)
                {
                    var depProject = solution.GetProject(depId);
                    if (depProject != null && depProject.FilePath != null)
                    {
                        depEntry.AddDependency(depProject.FilePath);
                    }
                }
                
                //Check for ASP.NET files
                if (ProjectContainsNETWebFiles(project))
                {
                    var pubProject = new PublishedWebProject(project, RuntimeInformation.RuntimeIdentifier, PUBLISH_CONFIG, dependencies, analysisOutput);
                    if (pubProject.IsSupportedSDK())
                    {
                        //If it is a supported SDK, publish the solution if it hasn't been
                        PublishSolution(solution, analysisOutput);
                        //Read in the corresponding ASP.NET components.
                        var foundTypes = new HashSet<SourceType>();
                        if (await pubProject.Analyze(foundTypes, analysisOutput))
                        {
                            dependencies.AddSourceTypes(foundTypes);
                        }
                    }
                    else
                    {
                        analysisOutput.AddErrorMessage("Found unsupported SDK in project: " + project.Name + " sdk: " + pubProject.SDK ?? "null");
                        //Just do the Roslyn analysis
                        await pubProject.Analyze();
                    }
                }
                else
                {
                    var roslynProject = new RoslynProject(project, dependencies, analysisOutput);
                    await roslynProject.Analyze();

                }
            }
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;

namespace DotNETDepends
{
    class SymbolDefinitionFinder : CSharpSyntaxWalker
    {
        private readonly DependencyEntry entry;

        public SymbolDefinitionFinder(DependencyEntry entry)
        {
            this.entry = entry;
        }

        public override void Visit(SyntaxNode? node)
        {
            if (node != null)
            {
                var symbol = entry.Semantic?.GetDeclaredSymbol(node);
                if (symbol != null && symbol.Kind == SymbolKind.NamedType)
                {
                    entry.Symbols.Add(symbol);
                }
            }
            base.Visit(node);
        }
    }

    class SolutionReader
    {
        private Dependencies dependencies = new Dependencies();
        public SolutionReader()
        {
            MSBuildLocator.RegisterDefaults();

        }

        public async Task ReadSolutionAsync(String path)
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(path).ConfigureAwait(false);
            var depGraph = solution.GetProjectDependencyGraph();
            var dependencyOrderedProjects = depGraph.GetTopologicallySortedProjects();
            foreach(var projectId in dependencyOrderedProjects)
            {
                await ProcessProjectAsync(projectId, solution, depGraph).ConfigureAwait(false);
            }
            await ResolveDepenedenciesAsync(solution).ConfigureAwait(false);
            dependencies.Print();
        }

        private async Task ResolveDepenedenciesAsync(Solution solution)
        {
            
            foreach(var entry in dependencies.GetFileEntries())
            {
                foreach(var symbol in entry.Symbols)
                {
                    var references = await SymbolFinder.FindReferencesAsync(symbol, solution).ConfigureAwait(false);
                    
                    foreach(var reference in references)
                    {
                        foreach(var location in reference.Locations)
                        {
                            if(!location.IsCandidateLocation && location.Location.IsInSource)
                            {
                                var path = location.Location.SourceTree.FilePath;
                                var sourceEntry = dependencies.GetEntry(path);
                                if(sourceEntry != null)
                                {
                                    sourceEntry.AddDependency(entry.FilePath);
                                }
                            }
                        }
                    }
                    
                }
            }
        }

        private async Task ProcessProjectAsync(ProjectId projectId, Solution solution, ProjectDependencyGraph depGraph)
        {
            var project = solution.GetProject(projectId);
            
            if (project != null && project.FilePath != null)
            {

                var depEntry = dependencies.CreateProjectEntry(project.FilePath);
                 
                var projectDependencies = depGraph.GetProjectsThatThisProjectDirectlyDependsOn(projectId);
                //Add all dependent projects to the entry
                foreach(var depId in projectDependencies)
                {
                    var depProject = solution.GetProject(depId);
                    if(depProject != null && depProject.FilePath != null)
                    {
                        depEntry.AddDependency(depProject.FilePath);
                    }
                }
                var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
                if (compilation != null)
                {
                    foreach (var tree in compilation.SyntaxTrees)
                    {
                        Console.WriteLine("Parsing: " + tree.FilePath);
                        //walk the syntaxtree to find referencable symbols
                        var fileDep = dependencies.CreateCodeFileEntry(tree.FilePath, compilation.GetSemanticModel(tree), tree);
                        SymbolDefinitionFinder walker = new SymbolDefinitionFinder(fileDep);
                        walker.Visit(fileDep.Tree?.GetRoot());
                    }
                }
                else
                {
                    Console.WriteLine("Compilation was null for " + project.FilePath);
                }
            }
        }
    }
}

using Disassembler;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETDepends
{
    /**
 * Locate declared types in a Roslyn syntax tree
 */
    internal class SymbolDefinitionFinder : SyntaxWalker
    {
        private readonly DependencyEntry entry;
        public SymbolDefinitionFinder(DependencyEntry entry) : base(SyntaxWalkerDepth.Node)
        {
            this.entry = entry;
        }

        public override void Visit(SyntaxNode? node)
        {
            if (node != null)
            {
                var symbol = entry.Semantic?.GetDeclaredSymbol(node);
                //Look for NamedTypes.  These are classes.
                if (symbol != null && symbol.Kind == SymbolKind.NamedType)
                {
                    entry.Symbols.Add(symbol);
                }
                //We can optimize this in the future to not descend in to uninteresting nodes
                base.Visit(node);
            }
        }

    }

    internal class RoslynProject
    {
        protected readonly Project project;
        private readonly IErrorReporter errorReporter;
        private readonly Dependencies dependencies;
        private readonly string? solutionRoot;
        public RoslynProject(Project project, Dependencies dependencies, IErrorReporter errorReporter)
        {
            this.project = project;
            this.dependencies = dependencies;
            this.errorReporter = errorReporter;
            solutionRoot = Path.GetDirectoryName(project.Solution.FilePath);

        }

        public async Task Analyze()
        {
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            if (compilation != null && solutionRoot != null)
            {

                foreach (var tree in compilation.SyntaxTrees)
                {

                    //Make sure we don't add generated files that can get pulled in.
                    if (tree.FilePath.StartsWith(solutionRoot))
                    {
                        //Register the file/type
                        var fileDep = dependencies.CreateCodeFileEntry(Path.GetRelativePath(solutionRoot, tree.FilePath), compilation.GetSemanticModel(tree), tree);
                        var walker = new SymbolDefinitionFinder(fileDep);
                        //walk the syntaxtree to find referencable symbols
                        walker.Visit(fileDep.Tree?.GetRoot());
                    }
                }
            }
            else
            {
                errorReporter.AddErrorMessage("Compilation was null for project: " + project.Name);
            }
        }

    }
}

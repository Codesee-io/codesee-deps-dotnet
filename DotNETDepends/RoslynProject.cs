using Disassembler;
using Microsoft.CodeAnalysis;

namespace DotNETDepends
{
    /**
 * Locate declared types in a Roslyn syntax tree
 */
    internal class SymbolDefinitionFinder : SyntaxWalker
    {
        private readonly DependencyEntry entry;
        private readonly SemanticModel semanticModel;
        public SymbolDefinitionFinder(DependencyEntry entry, SemanticModel semanticModel) : base(SyntaxWalkerDepth.Node)
        {
            this.entry = entry;
            this.semanticModel = semanticModel; 
        }

        public override void Visit(SyntaxNode? node)
        {
            if (node != null)
            {
                var symbol = semanticModel.GetDeclaredSymbol(node);

                //Look for NamedTypes.  These are classes defined in the file
                if (symbol != null && symbol.Kind == SymbolKind.NamedType)
                {
                    entry.Symbols.Add(symbol);

                }
                else
                {
                    //Look for references
                    var info = semanticModel.GetSymbolInfo(node);
                    
                    if (info.Symbol != null)
                    {
                        var sym = info.Symbol;
                        if (sym.Kind != SymbolKind.NamedType)
                        {
                            var type = sym.ContainingType;
                            if (type != null)
                            {
                                entry.AddReference(type);
                            }
                        }
                        else { entry.AddReference(sym); }

                    }

                }
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
                        var fileDep = dependencies.CreateCodeFileEntry(Path.GetRelativePath(solutionRoot, tree.FilePath));
                        var walker = new SymbolDefinitionFinder(fileDep, compilation.GetSemanticModel(tree));
                        //walk the syntaxtree to find referencable symbols
                        walker.Visit(tree.GetRoot());
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

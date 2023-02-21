using Microsoft.CodeAnalysis;

namespace DotNETDepends
{
    public enum EntryType
    {
        Project,
        File
    }

    /**
     * Entry class for a file to capture it's dependencies.
     */
    class DependencyEntry
    {
        public readonly String FilePath;
        public readonly EntryType Type;
        public HashSet<ISymbol> Symbols = new(SymbolEqualityComparer.Default);
        private readonly HashSet<string> References = new();

        //File dependencies of the file
        public HashSet<string> Dependencies { get; } = new HashSet<string>();

        public DependencyEntry(string filePath, EntryType type)
        {
            FilePath = filePath;
            Type = type;

        }

        public void AddReference(ISymbol symbol)
        {
            //ToDisplayString formats the symbol as <namespace>.<typeName>
            //stringifying this helps with the lookup, as we can't use a 
            //HashSet<ISymbol>::Contains to look them up
            References.Add(symbol.ToDisplayString());
        }

        public bool ReferencesSymbol(ISymbol symbol)
        {
            //ToDisplayString formats the symbol as <namespace>.<typeName>
            var synName = symbol.ToDisplayString();
            return References.Contains(synName);
        }

        public void AddDependency(string dependency)
        {
            if (dependency != FilePath)
            {
                Dependencies.Add(dependency);
            }
        }


    }
}

using Disassembler;
using Microsoft.CodeAnalysis;

namespace DotNETDepends
{
    /*
     * This class collects all of the reference entities.
     * I have projects and source files, as well as the
     * SourceTypes generated during disassembly.
     * It is used to resolve references afteer all type collection has
     * happened.  See SolutionReader::ResolveDependenciesAsync
     */
    class Dependencies
    {
        private Dictionary<string, DependencyEntry> entries = new();
        private List<SourceType> sourceTypes = new();

        public List<SourceType> SourceTypes { get { return sourceTypes; } }

        public DependencyEntry CreateProjectEntry(string filePath)
        {
            var dependency = new DependencyEntry(filePath, EntryType.Project);
            entries[filePath] = dependency;
            return dependency;
        }

        public DependencyEntry CreateCodeFileEntry(string filePath, SemanticModel semantic, SyntaxTree tree)
        {
            var dependency = new DependencyEntry(filePath, EntryType.File)
            {
                Semantic = semantic,
                Tree = tree
            };
            entries[filePath] = dependency;
            return dependency;
        }

        private List<DependencyEntry> GetEntriesByType(EntryType type)
        {
            List<DependencyEntry> result = new List<DependencyEntry>();
            foreach(var entry in entries.Values)
            {
                if(entry.Type == type)
                {
                    result.Add(entry);
                }
            }
            return result;
        }
        public List<DependencyEntry> GetFileEntries()
        {
            return GetEntriesByType(EntryType.File);
        }

        public DependencyEntry? GetEntry(string filePath)
        {
            entries.TryGetValue(filePath, out var entry);
            return entry;
        }

        public void AddSourceTypes(List<SourceType> types)
        {
            sourceTypes.AddRange(types);
        }

        /**
         * Search for references to the symbol in all of the SourceTypes
         * we have disassembled.
         */
        public List<SourceType> FindSourceSymbolReferences(ISymbol symbol)
        {
            var result = new List<SourceType>();
            foreach (var type in sourceTypes)
            {
                foreach(var typeRef in type.TypeReferences)
                {
                    if (typeRef.Name.Equals(symbol.Name) && typeRef.Namespace.Equals(symbol.ContainingNamespace.ToDisplayString())) 
                    {
                        result.Add(type);
                        break;
                    }
                }
            }
            return result;
        }

        /*
         * Get all references to symbol in the collection of SourceTypes.
         * This is resolving interdependencies between the disassebled types.
         */
        public List<SourceType> FindSourceSymbolReferences(SourceType symbol)
        {
            var result = new List<SourceType>();
            foreach (var type in sourceTypes)
            {
                if (!type.Path.Equals(symbol.Path))
                {
                    foreach (var typeRef in type.TypeReferences)
                    {
                        if (typeRef.Name.Equals(symbol.Name) && typeRef.Namespace.Equals(symbol.Namespace))
                        {
                            result.Add(type);
                            break;
                        }
                    }
                }
            }
            return result;
        }

        public void Print()
        {
            foreach(var entry in entries.Values)
            {
                foreach(var dep in entry.Dependencies)
                {
                    Console.WriteLine(entry.FilePath + " -> " + dep);
                }
            }

            foreach(var srcType in sourceTypes)
            {
                foreach(var dep in srcType.Dependencies)
                {
                    Console.WriteLine(srcType.Path+ " -> " + dep);
                }
            }
        }
    }
}

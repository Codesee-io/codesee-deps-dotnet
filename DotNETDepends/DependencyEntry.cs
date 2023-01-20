using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace DotNETDepends
{
    public enum EntryType
    {
        Project,
        File
    }

    class DependencyEntry
    {
        public readonly String FilePath;
        public readonly EntryType Type;
        public HashSet<ISymbol> Symbols = new(SymbolEqualityComparer.Default);
        public SemanticModel? Semantic { get; set; }
        public SyntaxTree? Tree { get; set; }

        public HashSet<string> Dependencies { get; } = new HashSet<string>();

        public DependencyEntry(string filePath, EntryType type)
        {
            FilePath = filePath;
            Type = type;
        }

        public void AddDependency(string dependency)
        {
            if(dependency != FilePath)
            {
                Dependencies.Add(dependency);
            }
        }

        
    }
}

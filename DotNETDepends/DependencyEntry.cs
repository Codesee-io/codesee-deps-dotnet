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

    /**
     * Entry class for a file to capture it's dependencies.
     */
    class DependencyEntry
    {
        public readonly String FilePath;
        public readonly EntryType Type;
        public HashSet<ISymbol> Symbols = new(SymbolEqualityComparer.Default);
       //This is the Rosalyn semantic model.  Only valid if Type == File.  
        public SemanticModel? Semantic { get; set; }
        //SyntaxTree of a C# or VB file.  Comes from Rosalyn.
        public SyntaxTree? Tree { get; set; }
        //File dependencies of the file
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

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace DotNETDepends
{
    class Dependencies
    {
        private Dictionary<string, DependencyEntry> entries = new Dictionary<string, DependencyEntry>();
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

        public DependencyEntry GetEntry(string filePath)
        {
            return entries[filePath];
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
        }
    }
}

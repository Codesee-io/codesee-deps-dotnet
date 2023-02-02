using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Disassembler
{
    /**
     * Used by the AssemblyReader to find types.
     * We map them in to namespaces to make it a bit more logarithmic.
     */
    public class SourceTypeLocator
    {
        private readonly SourceTypeBuilder builder;
        //Map of types in a namespace
        private readonly Dictionary<string, HashSet<SourceType>> types = new();

        /**
         * Gets all of the types we have added during disassembly
         */
        public HashSet<SourceType> SourceTypes
        {
            get
            {
                HashSet<SourceType> result = new();
                foreach (HashSet<SourceType> typeList in types.Values)
                {
                    foreach(var type in typeList)
                    {
                        result.Add(type);
                    }
                }
                return result;
            }
        }

        public SourceTypeLocator(SourceTypeBuilder builder)
        {
            this.builder = builder;

        }

        private void AddTypeToNamespace(string ns, SourceType type)
        {
            types.TryGetValue(ns, out HashSet<SourceType>? nsTypes);
            if (nsTypes == null)
            {
                nsTypes = new HashSet<SourceType>();
                types.Add(ns, nsTypes);
            }
            nsTypes.Add(type);
        }

        /**
         * Constructs the source type using the abstract SourceTypeBuilder
         * It then adds it to the types map by namespaces
         */
        public void AddSource(string filePath, string projectDir, string solutionDir, string rootNamepace)
        {
            var type = builder.Build(rootNamepace, filePath, projectDir, solutionDir);
            AddTypeToNamespace(type.Namespace, type);
            if(type.NamespaceAlias!= null)
            {
                AddTypeToNamespace(type.NamespaceAlias, type);  
            }
        }

        /**
         * Given a reflected Type object, find the corresponding SourceType,
         * which maps it back to a source file.
         */
        public SourceType? Locate(Type type)
        {
            types.TryGetValue(type.Namespace ?? "", out HashSet<SourceType>? nsTypes);
            if (nsTypes != null)
            {
                foreach (SourceType nsType in nsTypes)
                {
                    if (nsType.Name.Equals(type.Name))
                    {
                        return nsType;
                    }
                }
            }
            return null;
        }
    }
}

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
        private readonly Dictionary<string, List<SourceType>> types = new();

        /**
         * Gets all of the types we have added during disassembly
         */
        public List<SourceType> SourceTypes
        {
            get
            {
                List<SourceType> result = new();
                foreach (List<SourceType> typeList in types.Values)
                {
                    result.AddRange(typeList);
                }
                return result;
            }
        }

        public SourceTypeLocator(SourceTypeBuilder builder)
        {
            this.builder = builder;

        }

        /**
         * Constructs the source type using the abstract SourceTypeBuilder
         * It then adds it to the types map by namespaces
         */
        public void AddSource(string filePath, string projectDir, string solutionDir, string rootNamepace)
        {
            var type = builder.Build(rootNamepace, filePath, projectDir, solutionDir);
            types.TryGetValue(type.Namespace, out List<SourceType>? nsTypes);
            if (nsTypes == null)
            {
                nsTypes = new List<SourceType>();
                types.Add(type.Namespace, nsTypes);
            }
            nsTypes.Add(type);
        }

        /**
         * Given a reflected Type object, find the corresponding SourceType,
         * which maps it back to a source file.
         */
        public SourceType? Locate(Type type)
        {
            types.TryGetValue(type.Namespace ?? "", out List<SourceType>? nsTypes);
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

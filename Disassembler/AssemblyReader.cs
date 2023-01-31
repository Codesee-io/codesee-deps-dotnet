using System.Reflection;
using System.Runtime.InteropServices;

namespace Disassembler
{
    /**
     * Class responsible for reading types and type references from compiled assemblies
     */
    public class AssemblyReader
    {
        private readonly string path;
        private readonly SourceTypeLocator locator;


        public AssemblyReader(string assemblyPath, SourceTypeLocator locator)
        {
            path = assemblyPath;
            this.locator = locator;
        }
        /**
         * Loads an assembly and the:
         * 1.  Maps source files to top level types of the assembly
         * 2.  Processes all types for references and adds them to the SourceType object
         * 3.  Processes all method body IL code for further references
         */
        public bool Read()
        {
            var loader = new AssemblyLoader(path);

            Assembly? assembly = loader.Load();
            if (assembly != null)
            {
                foreach (var type in assembly.DefinedTypes)
                {
                    if (type.Assembly.Equals(assembly))
                    {
                        //Given the assembly type, locate it's source file and
                        //return a "SourceType" object mapping the file to namespace/type
                        var srcType = locator.Locate(type);
                        if (srcType != null)
                        {
                            srcType.AssemblyType = type;
                        }
                    }
                }
                foreach (var sourceType in locator.SourceTypes)
                {
                    //For every source type, process the found type from the assembly
                    //for references.  This walks the type definition for references as
                    //well as the IL code for method body references.
                    if (sourceType.AssemblyType != null)
                    {
                        ReferenceCollector refCollector = new(sourceType);
                        refCollector.CollectReferences();
                        refCollector.PrintReferences();
                    }
                    else
                    {
                        Console.WriteLine("Unresolved file: " + sourceType.Path);
                    }
                }

                return true;
            }
            return false;
        }

        /**
         * Returns the SourceTypes found during Read().
         * It will be empty if called before Read().
         */
        public List<SourceType> GetReadTypes()
        {
            return locator.SourceTypes;
        }
    }
}
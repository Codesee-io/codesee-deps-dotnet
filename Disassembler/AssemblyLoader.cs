using System.Reflection;
using System.Runtime.Loader;

namespace Disassembler
{
    /**
     * This class is responsible for resolving dependencies of 
     * our main assembly.  Since we use dotnet publish, all of the
     * dependencies are in the same folder as the main assemlby.
     */
    internal class LocalFolderLoadContext : AssemblyLoadContext
    {
        private readonly string assemblyDir;
        public LocalFolderLoadContext(string assemlbyDir)
        {
            this.assemblyDir = assemlbyDir;
        }
        /**
         * Loads the dependnent assembly from the folder of the main assembly
         */
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string assemblyPath = Path.Combine(assemblyDir, assemblyName.Name + ".dll");
            
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
            return null;
        }
    }

    internal class AssemblyLoader
    {
        private readonly string assemblyPath;
        private readonly LocalFolderLoadContext context;

        public AssemblyLoader(string assemblyPath) {
            this.assemblyPath = assemblyPath;
            context = new LocalFolderLoadContext(Path.GetDirectoryName(assemblyPath));
        }

        public Assembly? Load()
        {
            try
            {
                return context.LoadFromAssemblyPath(assemblyPath);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

using System.Xml;

namespace Disassembler
{
    public class CompilationInfo
    {
        public readonly string File;
        public readonly string Assembly;
        public readonly string Type;

        public CompilationInfo(string file, string assembly, string typeName)
        {
            Assembly = assembly;
            Type = typeName;
            File = file;
        }
    }

    /**
     * This class reads ".compiled" files that are generated with aspnet_compile.exe.
     * We don't use this right now, but if we support old ASP.NET on Windows we will.
     * ".compiled" files are small XML files that map source files to the generated
     * assembly.  When aspnet_compile builds, the assembly names are pseudo-random,
     * so we need the to map the sources to output assembly.
     */
    public class CompiledFileLocator
    {
        private string root;
        private string vRoot;
        public CompiledFileLocator(string outputRoot, string virtualRoot)
        {
            root = outputRoot;
            vRoot = virtualRoot;
        }

        private List<CompilationInfo> ReadCompiledFile(string file)
        {
            var result = new List<CompilationInfo>();
            XmlDocument doc = new();
            try
            {
                doc.Load(file);
                var preserve = doc.GetElementsByTagName("preserve");
                foreach (XmlNode node in preserve)
                {
                    var sourceFile = node.Attributes?.GetNamedItem("virtualPath")?.Value;
                    var assembly = node.Attributes?.GetNamedItem("assembly")?.Value;
                    var className = node.Attributes?.GetNamedItem("type")?.Value;
                    if (sourceFile != null &&
                        assembly != null && className != null)
                    {
                        result.Add(new CompilationInfo(Path.GetRelativePath(vRoot, sourceFile), assembly, className));   
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }
        public List<CompilationInfo> ReadCompiledFiles()
        {
            var result = new List<CompilationInfo>(); 
            var dirInfo = new DirectoryInfo(root);
            var files = dirInfo.GetFiles("**.compiled", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                result.AddRange(ReadCompiledFile(file.FullName));
            }
            return result;
        }
    }
}

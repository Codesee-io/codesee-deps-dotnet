using Disassembler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/*
 * This is a factory that create SourceTypeBuilder types based on the SDK.
 * There is one for Blazor-WASM and ASP.NET Core.
 * They are responsible for mapping sourcefiles to fully qualified type names.
 */
namespace DotNETDepends
{
    internal abstract class SourceTypeBuilderBase : SourceTypeBuilder
    {
        /*
         * Reads a namespace from a file if it has one
         */
        protected string? ReadNamespaceFromFile(string path)
        {
            try
            {
                string[] fileLines = System.IO.File.ReadAllLines(path);
                foreach (var line in fileLines)
                {
                    var lineContent = line.TrimStart();
                    if (lineContent.StartsWith("@namespace"))
                    {
                        string[] parts = lineContent.Split(' ');
                        if (parts.Length > 1)
                        {
                            for (var i = 1; i < parts.Length; i++)
                            {
                                var trimmed = parts[i].Trim();
                                if (trimmed.Length > 0)
                                {
                                    return trimmed;
                                }
                            }
                        }
                    }

                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        /*
         * In ASP and Bazor there is a magic import file.  In ASP it is "_ViewImports.cshtml" and "_Imports.razor for blazor.
         * If one of these is in a parent folder, it is automatically imported.  It can have a namespace declaration in it.
         * If it does, the namespace resets at the folder of the import.  Subsequent child folders are appended to the namespace.
         * 
         * Example:
         * \view\_Import.razor  -> sets namespace to codesee.views
         *     .\Welcome\Index.html  -> namespace is codesee.views.Welcome.
         *     
         * Not this is recursive up to parent folders
         */
        protected string? FindNamespaceFromViewImports(string startDir, string projectDir, string importFileName, string importFileExtension, out string? typeRoot)
        {
            typeRoot = null;
            var importsFile = Path.Combine(startDir, importFileName + importFileExtension);
            if (File.Exists(importsFile))
            {
                var ns = ReadNamespaceFromFile(importsFile);
                if (ns != null)
                {
                    typeRoot = startDir;
                    return ns;
                }
            }
            if (projectDir == startDir)
            {
                return null;
            }
            return FindNamespaceFromViewImports(Path.GetDirectoryName(startDir), projectDir, importFileName, importFileExtension, out typeRoot);

        }
        /*
         * Add any folders to the namespace if there was a magic import file.
         */
        protected string DetermineNamespaceFromPath(string path, string? rootNamespace, string projectDir)
        {
            var relativePath = Path.GetRelativePath(projectDir, path);
            relativePath = Path.GetDirectoryName(relativePath);
            if (relativePath.StartsWith(Path.DirectorySeparatorChar))
            {
                relativePath = relativePath.Substring(1);
            }
            if (relativePath.EndsWith(Path.DirectorySeparatorChar))
            {
                relativePath = relativePath[..^1];
            }
            relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '.');
            if (rootNamespace != null && rootNamespace.Length != 0)
            {
                if (relativePath.Length > 0)
                {
                    return rootNamespace + "." + relativePath;
                }
                else
                {
                    return rootNamespace;
                }
            }

            return relativePath;
        }
    }

    /*
     * Blazor flavor of the SourceTypeBuilder.  Mainly has a different file name for the magic import file.
     */
    internal class BlazorWASMSourceTypeBuider : SourceTypeBuilderBase
    {
        public override SourceType Build(string? rootNamespace, string filePath, string projectDir, string solutionDir)
        {
            var ns = ReadNamespaceFromFile(filePath);
            if (ns == null)
            {
                ns = FindNamespaceFromViewImports(Path.GetDirectoryName(filePath), projectDir, "_Imports", ".razor", out string? typeRoot);
                ns = DetermineNamespaceFromPath(filePath, ns ?? rootNamespace, typeRoot ?? projectDir);
            }
            ns ??= "";
            var typeName = Path.GetFileNameWithoutExtension(filePath);
            return new SourceType(typeName, Path.GetRelativePath(solutionDir, filePath), ns);
        }
    }

    /*
     * ASP.NET core flavor of the SourceTypeBuilder.  Has a different file name for the magic import file.
     * The generated type names are also different.
     */
    internal class ASPNetCoreSourceTypeBuilder : SourceTypeBuilderBase
    {

        public override SourceType Build(string? rootNamespace, string filePath, string projectDir, string solutionDir)
        {
            var ns = ReadNamespaceFromFile(filePath);
            if (ns == null)
            {
                ns = FindNamespaceFromViewImports(Path.GetDirectoryName(filePath), projectDir, "_ViewImports", Path.GetExtension(filePath), out string? typeRoot);
                ns = DetermineNamespaceFromPath(filePath, ns ?? rootNamespace, typeRoot ?? projectDir);
            }
            ns ??= "";
            var typeName = GetTypeName(filePath, projectDir);
            return new SourceType(typeName, Path.GetRelativePath(solutionDir, filePath), ns, "AspNetCoreGeneratedDocument");
        }

        /*
         * ASP uses most of the path in the Class name.  Blazor just uses the file name as Class name.
         */
        private string GetTypeName(string filePath, string projectDir)
        {
            var relativeFile = Path.GetRelativePath(projectDir, filePath);
            var relativeDir = Path.GetDirectoryName(relativeFile);
            if (relativeDir.StartsWith(Path.DirectorySeparatorChar))
            {
                relativeDir = relativeDir.Substring(1);
            }
            if (relativeDir.EndsWith(Path.DirectorySeparatorChar))
            {
                relativeDir = relativeDir[..^1];
            }
            return relativeDir.Replace(Path.DirectorySeparatorChar, '_') + "_" + Path.GetFileNameWithoutExtension(filePath);
        }
    }
    internal class SourceTypeBuilderFactory
    {
        public static SourceTypeBuilder? Create(string sdk)
        {
            switch (sdk)
            {
                case SupportedSdks.ASP_NET_CORE:
                    return new ASPNetCoreSourceTypeBuilder();
                case SupportedSdks.BLAZOR_WASM:
                    return new BlazorWASMSourceTypeBuider();
            }
            return null;
        }
    }
}

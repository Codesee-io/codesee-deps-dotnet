using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Disassembler
{
    /**
     * A source type represents a disassembled class we have mapped to a source file.
     */
    public class SourceType
    {
        //AspNetCore will generate in an "AspNetCoreGeneratedDocuments" namespace sometimes
        //We use the alsias to determine if this type may be in 1 of 2 location.
        public string? NamespaceAlias;
        //Type name
        public readonly string Name;
        //Solution relative file path
        public readonly string Path;
        //.NET namespace
        public readonly string Namespace;
        //Any types this type references
        public readonly HashSet<Type> TypeReferences = new();
        //File dependencies based on TypeReferences
        public readonly HashSet<string> Dependencies = new();
        //The type we mapped the source to in the compiled assembly
        public Type? AssemblyType { get; set; }

        public SourceType(string name, string path, string ns, string? nsAlias = null) { 
            Name = name;
            Path = path;
            Namespace = ns;
            NamespaceAlias = nsAlias;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Disassembler
{
    /**
     * Base class use to map source files to compiled types.
     * There are specific implementations depending on the SDK.
     * See DotNetDepends/SourceTypeBuilderFactory
     */
    public abstract class SourceTypeBuilder
    {        
        public abstract SourceType Build(string? rootNamespace, string filePath, string projectDir, string solutionDir);
    }
}

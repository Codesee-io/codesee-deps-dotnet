using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDisassembler
{
    internal class TestUtils
    {
        public static string? GetFixturesPath()
        {
            return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Fixtures");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDependencyReading
{
    internal class TestUtils
    {
        public static string? GetFixturesPath()
        {
            string workingDir =  Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            while(workingDir != null && !workingDir.EndsWith("TestDependencyReading")) {
                workingDir= Path.GetDirectoryName(workingDir);
            }
            return Path.Combine(workingDir, "Fixtures");
        }
    }
}

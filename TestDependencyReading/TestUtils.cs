using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDependencyReading
{
    internal class TestUtils
    {
        public static string GetFixturesPath()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            if (assembly != null && assembly.Location != null)
            {
                string? workingDir = Path.GetDirectoryName(assembly.Location);
                while (workingDir != null && !workingDir.EndsWith("TestDependencyReading"))
                {
                    workingDir = Path.GetDirectoryName(workingDir);
                }
                return Path.Combine(workingDir ?? "", "Fixtures");
            }
            throw new Exception("Unable to determine fixture directory.");
        }
    }
}

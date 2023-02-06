using DotNETDepends;
using DotNETDepends.Output;
using Microsoft.Build.Locator;
using System.Runtime.InteropServices;

namespace TestDependencyReading
{
    [TestClass]
    public class DependencyReaderTests
    {
        private void AssertLinks(Dictionary<string, string> expected, Link[] found)
        {
            foreach (var link in found)
            {
                expected.TryGetValue(link.From, out var expectedTo);
                Assert.IsNotNull(expectedTo);
                Assert.AreEqual(expectedTo, link.To);
            }
        }

        private bool LinkMatches(string from, string to, Link link)
        {
            return link.To.Equals(FixPath(to)) && link.From.Equals(FixPath(from));
        }

        string FixPath(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return path.Replace('/', '\\');
            }
            else
            {
                return path.Replace('\\', '/');
            }
        }
        [TestInitialize]
        public void Initialize()
        {
            if (!MSBuildLocator.IsRegistered)
            {
                //This finds the MSBuild libs from the .NET SDK
                MSBuildLocator.RegisterDefaults();
            }

        }
        [TestMethod]
        public async Task TestASPNETCoreMVCSolution()
        {
            var slnFile = Path.Combine(TestUtils.GetFixturesPath(), "NetCoreMVC", "NetCoreMVC.sln");

            AnalysisOutput output = new();
            SolutionReader reader = new();
            await reader.ReadSolutionAsync(slnFile, output).ConfigureAwait(false);
            Assert.IsNotNull(output);
            Assert.AreEqual(2, output.Links.Length);
            AssertLinks(new Dictionary<string, string>
            {
                { FixPath("NetCoreMVC\\Controllers\\HomeController.cs"),
                  FixPath("NetCoreMVC\\Models\\ErrorViewModel.cs")
                },
                { FixPath("NetCoreMVC\\Views\\Shared\\Error.cshtml"),
                  FixPath("NetCoreMVC\\Models\\ErrorViewModel.cs")
                }
            }, output.Links);

        }
        [TestMethod]
        public async Task TestWindowsVBSolution()
        {
            var slnFile = Path.Combine(TestUtils.GetFixturesPath(), "VBWinApp", "WinFormsApp1.sln");
            AnalysisOutput output = new();
            SolutionReader reader = new();
            await reader.ReadSolutionAsync(slnFile, output).ConfigureAwait(false);
            Assert.IsNotNull(output);
            Assert.AreEqual(3, output.Links.Length);

            int matches = 0;
            foreach (var link in output.Links)
            {
                if (LinkMatches("WinFormsApp1\\Form1.vb", "WinFormsApp1\\DoSomething.vb", link))
                {
                    matches++;
                    continue;
                }
                if (LinkMatches("WinFormsApp1\\My Project\\Application.Designer.vb",
                "WinFormsApp1\\Form1.Designer.vb", link))
                {
                    matches++;
                    continue;
                }
                if (LinkMatches("WinFormsApp1\\My Project\\Application.Designer.vb",
                "WinFormsApp1\\Form1.vb", link))
                {
                    matches++;
                    continue;
                }
            }
            Assert.AreEqual(3, matches);
        }

        [TestMethod]
        public async Task TestWindowsCSharpSolution()
        {
            var slnFile = Path.Combine(TestUtils.GetFixturesPath(), "CSharpWinApp", "CSharpWinApp.sln");
            AnalysisOutput output = new();
            SolutionReader reader = new();
            await reader.ReadSolutionAsync(slnFile, output).ConfigureAwait(false);
            Assert.IsNotNull(output);
            Assert.AreEqual(3, output.Links.Length);
            int matches = 0;
            foreach (var link in output.Links)
            {
                if (LinkMatches("CSharpWinApp\\Form1.cs", "CSharpWinApp\\DoSomething.cs", link))
                {
                    matches++;
                    continue;
                }
                if (LinkMatches("CSharpWinApp\\Program.cs",
                "CSharpWinApp\\Form1.cs", link))
                {
                    matches++;
                    continue;
                }
                if (LinkMatches("CSharpWinApp\\Program.cs",
                "CSharpWinApp\\Form1.Designer.cs", link))
                {
                    matches++;
                    continue;
                }
            }
            Assert.AreEqual(3, matches); ;

        }

    }
}
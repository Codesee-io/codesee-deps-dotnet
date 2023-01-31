using Disassembler;

namespace TestDisassembler
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestCompilationFileLocator()
        {
            var fixtures = TestUtils.GetFixturesPath();
            Assert.IsNotNull(fixtures);
            CompiledFileLocator locator = new CompiledFileLocator(fixtures, "/temp");
            var result = locator.ReadCompiledFiles();
            Assert.AreEqual(result.Count, 3);

        }
    }
}
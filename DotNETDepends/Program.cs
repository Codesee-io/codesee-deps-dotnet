
namespace DotNETDepends;
class Program
{
    static async Task Main(string[] args)
    {
        var reader = new SolutionReader();
        await reader.ReadSolutionAsync(args[0]);

    }
}
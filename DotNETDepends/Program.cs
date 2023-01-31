
namespace DotNETDepends;
class Program
{
    /**
     * Entry point.  Just takes a solution file as arg[0]
     * See the documentation on SolutionReader for an 
     * overview of processing.
     */
    static async Task Main(string[] args)
    {
        if (args.Length == 1 && File.Exists(args[0]))
        {
            var reader = new SolutionReader();
            await reader.ReadSolutionAsync(args[0]).ConfigureAwait(false);
            //Set this to indicate success
            Environment.ExitCode = 0;
        }
        else
        {
            Console.WriteLine("Invalid arguments.  Solution could not be found.");
            Console.WriteLine("Usage: DotNetDepends <Path to solution (.sln file)>");
            Environment.Exit(1);
        }

    }
}
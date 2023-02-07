
using DotNETDepends.Output;
using Microsoft.Build.Locator;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotNETDepends;
class Program
{
    static bool ValidateFileNameInput(string fileName)
    {
        return (File.Exists(fileName) && fileName.EndsWith("sln"));
    }
    /**
     * Entry point.  Just takes a solution file as arg[0]
     * See the documentation on SolutionReader for an 
     * overview of processing.
     */
    static async Task Main(string[] args)
    {
        if (args.Length == 1 && File.Exists(args[0]))
        {
            if ("--version".Equals(args[0]))
            {
                PrintVersion();
                Environment.Exit(0);
                return;
            }
            if (!ValidateFileNameInput(args[0]))
            {
                throw new Exception("Invalid command line argument.");
            }
            AnalysisOutput output = new();
            try
            {
                //This finds the MSBuild libs from the .NET SDK
                //It can only be called once without throwing
                MSBuildLocator.RegisterDefaults();

                var reader = new SolutionReader();
                await reader.ReadSolutionAsync(args[0], output).ConfigureAwait(false);
                //Set this to indicate success
                Environment.ExitCode = 0;
            }catch(Exception ex)
            {
                output.AddErrorMessage(ex.ToString());
            }
            Console.WriteLine("__codesee.output.begin__");
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(output, options);
            Console.WriteLine(jsonString);
        }
        else
        {
            Console.WriteLine("Invalid arguments.  Solution could not be found.");
            Console.WriteLine("Usage: DotNetDepends <Path to solution (.sln file)>");
            Environment.Exit(1);
        }

    }
    private static void PrintVersion()
    {
        //!!When you change this version be sure to update the expected version in dot-net.ts in the codesee repo!!
        Console.WriteLine("0.1.0");
    }
    private static bool ValidateFileNameInput(string v)
    {
        throw new NotImplementedException();
    }
}
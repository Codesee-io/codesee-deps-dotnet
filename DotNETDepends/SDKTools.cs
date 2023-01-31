using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
namespace DotNETDepends
{
    /**
     * Not much here.  We may need other tools from the Sdk in the future.
     */
    internal class SDKTools
    {

        public static string GetDotnetPath()
        {
            return "dotnet";
        }
    }
}


namespace DotNETDepends
{
    /*
     * SDKs we support as PublishedProjects (for disassemlby)
     */
    internal static class SupportedSdks
    {
        public const string ASP_NET_CORE = "Microsoft.NET.Sdk.Web";
        public const string BLAZOR_WASM = "Microsoft.NET.Sdk.BlazorWebAssembly";

        public static bool IsSupportedSDK(string sdk)
        {
            return sdk == ASP_NET_CORE || sdk == BLAZOR_WASM;
        }
    }



}

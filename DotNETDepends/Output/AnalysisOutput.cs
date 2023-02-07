
using Disassembler;

namespace DotNETDepends.Output
{
    public class Link
    {
        public Link(string from, string to)
        {
            this.from = from;
            this.to = to;
        }
        //When from and to are deserialized by Node, it expects lower case
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Compatibility")]
        public string from { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Compatibility")]
        public string to { get; set; }
    }

    public class AnalysisOutput : IErrorReporter
    {
        public Link[] Links { get { return _links.ToArray(); } }
        public string[] Errors { get { return _errors.ToArray(); } }

        private readonly List<string> _errors = new();
        private readonly List<Link> _links = new();

        public void AddLink(Link link)
        {
            _links.Add(link);
        }

        public void AddErrorMessage(string message)
        {
            _errors.Add(message);
        }
    }
}

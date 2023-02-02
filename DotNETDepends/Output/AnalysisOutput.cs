
using Disassembler;

namespace DotNETDepends.Output
{
    internal class Link
    {
        public Link(string from, string to)
        {
            From = from;
            To = to;
        }
        public string From { get; set; }
        public string To { get; set; }
    }
    internal class AnalysisOutput : IErrorReporter
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

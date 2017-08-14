using System.IO;
using System.Text;

namespace CodeProxy.Http
{
    internal class HttpResponseData
    {
        public Stream Content { get; set; }

        public Encoding Encoding { get; set; }

        public string MimeType { get; set; }
    }
}

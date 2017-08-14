using System;
using System.IO;
using System.Text;

namespace CodeProxy.Http
{
    public interface IMediaSerializer
    {
        object Deserialize(Type type, string mimeType, Encoding encoding, Stream content);
        T Deserialize<T>(string mimeType, Encoding encoding, Stream content);
        void Serialize(Type type, object content, string mimeType, Encoding encoding, Stream output);
    }
}

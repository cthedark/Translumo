using System.Net;
using Translumo.Translation.Configuration;
using Translumo.Utils.Http;

namespace Translumo.Translation.Local
{
    public sealed class LocalContainer : TranslationContainer
    {
        public HttpReader Reader { get; set; }

        public LocalContainer(Proxy proxy = null, bool isPrimary = false) : base(proxy, isPrimary)
        {
            Reader = CreateReader(proxy);
        }

        public override void Block()
        {
            base.Block();
            Reader.Cookies = new CookieContainer();
        }

        private HttpReader CreateReader(Proxy proxy)
        {
            var httpReader = new HttpReader();

            httpReader.ContentType = "application/json";
            httpReader.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36";
            httpReader.Accept = "*/*";

            return httpReader;
        }
    }
}

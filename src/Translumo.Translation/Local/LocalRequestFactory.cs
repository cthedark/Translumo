namespace Translumo.Translation.Local
{
// This is now deprecated since the request itself is a free form text that is user-configurable.
    internal static class LocalRequestFactory
    {
        public static LocalRequest CreateRequest(LocalContainer container, string text, string sourceLangCode, string targetLangCode)
        {
            return new LocalRequest()
            {
                Body = new LocalRequest.LocalRequestBody()
                {
                    Query = text,
                    Source = sourceLangCode,
                    Target = targetLangCode,
                    Format = "text",
                    Alternatives = 0,
                    ApiKey = ""
                }
            };
        }
    }
}

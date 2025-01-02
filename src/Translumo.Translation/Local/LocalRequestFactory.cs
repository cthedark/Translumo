namespace Translumo.Translation.Local
{
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

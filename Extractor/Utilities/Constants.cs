namespace Extractor.Utilities
{
    internal static class Constants
    {
        internal static readonly string YoutubeBaseUrl = "https://www.youtube.com";
        internal static readonly string YoutubeV1Url = $"{YoutubeBaseUrl}/youtubei/v1";

        internal static readonly string SearchCompletionUrl = "https://suggestqueries-clients6.youtube.com/complete/search";

        internal static readonly string ApiKeyQueryKey = "key";
        internal static readonly string DefaultApiKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";

        internal static readonly string ClientNameHeaderKey = "x-youtube-client-name";
        internal static readonly string DefaultClientName = "1"; // WEB

        internal static readonly string ClientVersionHeaderKey = "x-youtube-client-version";
        internal static readonly string DefaultClientVersion = "2.20251030.01.00";

        internal static readonly string UserAgentHeaderKey = "user-agent";
        internal static readonly string DefaultUserAgent = " Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36";

        internal static readonly string PrettyPrintQueryKey = "prettyPrint";
        internal static readonly string DefaultPrettyPrintValue = "false";

        internal static readonly string OriginHeaderKey = "Origin";
        internal static readonly string DefaultOrigin = "https://www.youtube.com";

        internal static readonly string RefererHeaderKey = "Referer";
        internal static readonly string DefaultReferer = "https://www.youtube.com/";
        
        internal static readonly string XGoogVisitorIdHeaderKey = "X-Goog-Visitor-Id";

        internal static readonly string DefaultBaseJsUrl = "https://www.youtube.com/s/player/3cd2d050/player_ias.vflset/en_GB/base.js";
        internal static readonly string DefaultThrottlingDecryptorFunctionName = "Qla";
    }
}

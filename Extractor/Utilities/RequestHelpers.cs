using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Web;

namespace Extractor.Utilities
{
	internal static class RequestHelpers
	{
		internal static Uri GetYoutubeV1Uri(string endpoint)
		{
			var uriBuilder = new UriBuilder($"{Constants.YoutubeV1Url}/{endpoint}");
			var queryBuilder = HttpUtility.ParseQueryString(uriBuilder.Query);
			queryBuilder.AddQueryStrings(new Dictionary<string, string>
			{
				{ Constants.PrettyPrintQueryKey, Constants.DefaultPrettyPrintValue }
			});
			uriBuilder.Query = queryBuilder.ToString();

			return uriBuilder.Uri;
		}

		internal static Uri GetSearchCompletionUri(string searchText, CultureInfo culture)
		{
			var uriBuilder = new UriBuilder(Constants.SearchCompletionUrl);
			var queryBuilder = HttpUtility.ParseQueryString(uriBuilder.Query);
			queryBuilder.AddQueryStrings(new Dictionary<string, string>
			{
				{ "client", "youtube" },
				{ "hl", culture.Name },
				{ "gl", culture.GetCountryCode() },
				{ "ds", "yt" },
				{ "q", searchText },
				{ "xhr", "t" }
			});
			uriBuilder.Query = queryBuilder.ToString();

			return uriBuilder.Uri;
		}

		internal static HttpRequestMessage AddYoutubeV1Headers(this HttpRequestMessage request, string visitorId)
		{
			request.Headers.Add(Constants.ClientNameHeaderKey, Constants.DefaultClientName);
			request.Headers.Add(Constants.ClientVersionHeaderKey, Constants.DefaultClientVersion);
			request.Headers.Add(Constants.OriginHeaderKey, Constants.DefaultOrigin);
			request.Headers.Add(Constants.RefererHeaderKey, Constants.DefaultReferer);

			if (visitorId != null)
			{
				request.Headers.Add(Constants.XGoogVisitorIdHeaderKey, visitorId);
			}
			
			return request;
		}

		private static JsonObject CommonYoutubeV1EndpointBody(CultureInfo culture) {
			return new JsonObject
			{
				["context"] = new JsonObject
				{
					["client"] = new JsonObject
					{
						["hl"] = culture.Name,
						["gl"] = culture.GetCountryCode(),
						["clientName"] = "WEB",
						["clientVersion"] = Constants.DefaultClientVersion,
						["originalUrl"] = Constants.YoutubeBaseUrl,
						["platform"] = "DESKTOP",
					},
					["user"] = new JsonObject
					{
						["lockedSafetyMode"] = false
					},
					["request"] = new JsonObject
					{
						["useSsl"] = true,
						["internalExperimentFlags"] = new JsonArray()
					}
				}
			};
		}

		internal static HttpRequestMessage AddYoutubeV1EndpointBody(this HttpRequestMessage request, CultureInfo culture, Dictionary<string, JsonNode> additionalFields)
		{
			var body = CommonYoutubeV1EndpointBody(culture);
			foreach (var fields in additionalFields)
			{
				body.Add(fields);
			}

			request.Content = body.ToJsonStringContent();
			return request;
		}

		internal static void AddQueryStrings(this NameValueCollection query, Dictionary<string, string> keyValuePairs)
		{
			foreach (var keyValuePair in keyValuePairs)
			{
				query.Add(keyValuePair.Key, keyValuePair.Value);
			}
		}
	}
}

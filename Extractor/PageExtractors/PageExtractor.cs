using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Extractor.Utilities;

namespace Extractor.PageExtractors
{
	public abstract class PageExtractor
	{
		protected string VisitorId { get; set; }
		protected HttpClient Client { get; }
		protected CultureInfo Culture { get; }
		protected bool useVisitorId { get; private set; }

		public PageExtractor(HttpClient client, CultureInfo culture)
		{
			Client = client;
			Culture = culture;
		}

		public PageExtractor(HttpClient client) : this(client, Thread.CurrentThread.CurrentCulture) { }
		
		/// <summary>
		/// Fetch Contents
		/// <param name="endpoint">Endpoint</param>
		/// <param name="additionalFields">Additional data to pass to request</param>
		/// </summary>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="JsonException"></exception>
		protected async Task<JsonObject> FetchContentsAsync(string endpoint,  Dictionary<string, JsonNode> additionalFields)
		{
			using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, RequestHelpers.GetYoutubeV1Uri(endpoint)))
			{
				requestMessage.AddYoutubeV1Headers(VisitorId)
					.AddYoutubeV1EndpointBody(Culture, additionalFields);

				using (var response = await Client.SendAsync(requestMessage))
				{
					response.EnsureSuccessStatusCode();
					var deserializedResponse = await response.Content.DeserializeAsync<JsonObject>();
					
					if (useVisitorId && deserializedResponse.TryGetArray("responseContext.serviceTrackingParams", out var trackingParams))
					{
						var newVisitorId = trackingParams.FirstOrDefault(trackingParam =>
								trackingParam.Get<string>("service") == "GFEEDBACK")
							?.GetArray("params")
							.FirstOrDefault(param => param.Get<string>("key") == "visitor_data")
							?.Get<string>("value");
						
						if  (newVisitorId != null)
							VisitorId = newVisitorId;
					}
					
					return deserializedResponse;
				}
			}
		}
		
		/// <summary>
		/// Fetch Continuation Contents
		/// <param name="endpoint">Endpoint</param>
		/// <param name="continuationToken">Continuation Token</param>
		/// </summary>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="JsonException"></exception>
		protected Task<JsonObject> FetchContinuationDataAsync(string endpoint, string continuationToken)
		{
			return FetchContentsAsync(endpoint, new Dictionary<string, JsonNode> { { "continuation", continuationToken } });
		}
		
		public void UseVisitorId(string visitorId = null)
		{
			useVisitorId = true;
			VisitorId = visitorId;
		}
	}
}

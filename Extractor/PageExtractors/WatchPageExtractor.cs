using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Extractor.Exceptions;
using Extractor.ItemCollectors;
using Extractor.ItemExtractors;
using Extractor.Models;
using Extractor.Utilities;

namespace Extractor.PageExtractors
{
	public class WatchPageExtractor : PageExtractor
	{
		private static readonly string s_playerEndpoint = "player";
		private static readonly string s_nextEndpoint = "next";

		public WatchPageExtractor(HttpClient client) : base(client) { }
		public WatchPageExtractor(HttpClient client, CultureInfo culture) : base(client, culture) { }

		/// <summary>
		/// Fetch Data
		/// </summary>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="JsonException"></exception>
		private async Task<JsonObject> FetchDataAsync(string endpoint, string videoId)
		{
			using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, RequestHelpers.GetYoutubeV1Uri(endpoint, ApiKey)))
			{
				requestMessage.AddYoutubeV1Headers()
					.AddYoutubeV1EndpointBody(Culture, new Dictionary<string, JsonNode> { { "videoId", videoId } });

				using (var response = await Client.SendAsync(requestMessage))
				{
					response.EnsureSuccessStatusCode();
					return await response.Content.DeserializeAsync<JsonObject>();
				}
			}
		}

		/// <summary>
		/// Get Watch Page Contents
		/// </summary>
		/// <param name="videoId">Video Id</param>
		/// <returns>Watch page contents</returns>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="JsonException"></exception>
		/// <exception cref="ParsingException"></exception>
		public async Task<WatchPageContents> GetContentsAsync(string videoId)
		{
			var playerResponse = await FetchDataAsync(s_playerEndpoint, videoId);
			var nextResponse = await FetchDataAsync(s_nextEndpoint, videoId);

			var streamInfoExtractor = new StreamInfoExtractor(playerResponse, nextResponse);
			var streamInfoCollector = new StreamInfoCollector();
			streamInfoCollector.Collect(streamInfoExtractor);
			var watchPageContents = new WatchPageContents
			{
				StreamInfo = streamInfoCollector.Items.First(),
				Exceptions = streamInfoCollector.Exceptions
			};

			try
			{
				watchPageContents.TotalCommentsCount = GetCommentsCount(nextResponse);
				watchPageContents.CommentsContinuationToken = GetCommentContinuationTokenFromInitialNextResponse(nextResponse);
			}
			catch (Exception e)
			{
				watchPageContents.Exceptions.Add(new ParsingException("Unable to get \"Comments\" in watch page", e));
			}

			try
			{
				watchPageContents.WatchNextContinuationToken = GetNextWatchContinuationToken(GetSecondaryResults(nextResponse));
				var streamItemCollector = new StreamItemCollector();
				streamItemCollector.Collect(
					GetSecondaryResults(nextResponse)
					.Where(r => r.Has("compactVideoRenderer"))
					.Select(r => new StreamItemExtractor(r.GetObject("compactVideoRenderer")))
				);
				watchPageContents.NextWatchItems = streamItemCollector.Items;
				watchPageContents.Exceptions.AddRange(streamItemCollector.Exceptions);
			}
			catch (Exception e)
			{
				watchPageContents.Exceptions.Add(new ParsingException("Unable to get \"Next to Watch contents\" in watch page", e));
			}

			return watchPageContents;
		}

		/// <summary>
		/// Get Next Watch Continuation Contents
		/// </summary>
		/// <param name="continuationToken">Continuation Token</param>
		/// <returns>Watch Page Contents with Next Watch contents only</returns>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="JsonException"></exception>
		/// <exception cref="ParsingException"></exception>
		public async Task<WatchPageContents> GetNextWatchContinuationContentsAsync(string continuationToken)
		{
			var response = await FetchContinuationDataAsync(s_nextEndpoint, continuationToken);
			var watchNextResults = response.GetArray("onResponseReceivedEndpoints").GetObject(0).GetArray("appendContinuationItemsAction.continuationItems");
			var streamItemCollector = new StreamItemCollector();
			streamItemCollector.Collect(
				watchNextResults
				.Where(r => r.Has("compactVideoRenderer"))
				.Select(r => new StreamItemExtractor(r.GetObject("compactVideoRenderer")))
			);
			return new WatchPageContents
			{
				NextWatchItems = streamItemCollector.Items,
				Exceptions = streamItemCollector.Exceptions,
				WatchNextContinuationToken = GetNextWatchContinuationToken(watchNextResults)
			};
		}

		/// <summary>
		/// Get Comments Continuation Contents
		/// </summary>
		/// <param name="continuationToken">Continuation Token</param>
		/// <returns>Watch Page Contents with comments only</returns>
		/// <exception cref="HttpRequestException"></exception>
		/// <exception cref="JsonException"></exception>
		/// <exception cref="ParsingException"></exception>
		public async Task<WatchPageContents> GetCommentsContinuationContentsAsync(string continuationToken)
		{
			var response = await FetchContinuationDataAsync(s_nextEndpoint, continuationToken);
			var comments = response.GetArray("onResponseReceivedEndpoints").GetObject(1).GetArray("reloadContinuationItemsCommand.continuationItems");
			var commentItemCollector = new CommentItemCollector();
			commentItemCollector.Collect(
				comments
				.Where(c => c.Has("commentThreadRenderer"))
				.Select(r => new CommentItemExtractor(r.GetObject("commentThreadRenderer")))
			);
			return new WatchPageContents
			{
				Comments = commentItemCollector.Items,
				Exceptions = commentItemCollector.Exceptions,
				WatchNextContinuationToken = ParsingHelpers.ParseContinuationItemRenderer(comments
					.LastOrDefault(c => c.Has("continuationItemRenderer"))?.GetObject("continuationItemRenderer"))
			};
		}


		private static string GetCommentsCount(JsonObject nextResponse)
		{
			return ParsingHelpers.GetText(GetResultsContents(nextResponse)
				.First(i => i.TryGetArray("itemSectionRenderer.contents", out var contents) &&
				            contents.GetObject(0).Has("commentsEntryPointHeaderRenderer"))
				.GetArray("itemSectionRenderer.contents").GetObject(0)
				.GetObject("commentsEntryPointHeaderRenderer.commentCount"));
		}

		private string GetCommentContinuationTokenFromInitialNextResponse(JsonObject nextResponse)
		{
			return ParsingHelpers.ParseContinuationItemRenderer(GetResultsContents(nextResponse).Last()
				.GetArray("itemSectionRenderer.contents")
				.GetObject(0)
				.GetObject("continuationItemRenderer"));
		}

		private static JsonArray GetResultsContents(JsonObject nextResponse)
		{
			return nextResponse.GetArray("contents.twoColumnWatchNextResults.results.results.contents");
		}

		private static JsonArray GetSecondaryResults(JsonObject nextResponse)
		{
			return nextResponse.GetArray("contents.twoColumnWatchNextResults.secondaryResults.secondaryResults.results");
		}

		private static string GetNextWatchContinuationToken(JsonArray watchNextResults)
		{
			return ParsingHelpers.ParseContinuationItemRenderer(watchNextResults
				.LastOrDefault(w => w.Has("continuationItemRenderer"))
				?.GetObject("continuationItemRenderer"));
		}
	}
}
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Extractor.Exceptions;
using Extractor.ItemExtractors;
using Extractor.ItemExtractors.Interface;
using Extractor.Models;
using Extractor.Models.Enums;

namespace Extractor.Utilities
{
	internal static class ParsingHelpers
	{
		/// <summary>
		/// Extracts Thumbnail Info from thumbnails
		/// </summary>
		/// <param name="thumbnails">Thumbnails Object</param>
		/// <returns>ThumbnailInfo[]</returns>
		/// <exception cref="ParsingException"></exception>
		internal static ThumbnailInfo[] ParseThumbnails(JsonArray thumbnails)
		{
			return thumbnails
				.Select(t => new ThumbnailInfo {
					Url = t.Get<string>("url"),
					Width = t.Get<int>("width"),
					Height = t.Get<int>("height")
				})
				.ToArray();
		}

		/// <summary>
		/// Extracts Stream Item from richSectionRenderer
		/// </summary>
		/// <param name="richSectionRenderer">Rich Section Renderer Object</param>
		/// <returns>List of IStreamItemExtractor with the title of the section</returns>
		/// <exception cref="ParsingException"></exception>
		internal static (string title, List<IStreamItemExtractor> streamItemExtractors) ParseRichSectionRenderer(JsonObject richSectionRenderer)
		{
			if (!richSectionRenderer.TryGetObject("content.richShelfRenderer", out var shelf)) return default;
			
			var title = GetText(shelf.GetObject("title"));
			var contents = shelf.GetArray("contents");
			var items = new List<IStreamItemExtractor>();
			foreach (var contentItem in contents)
			{
				var content = contentItem.GetObject("richItemRenderer.content");
				if (content
				    .TryGetOneOf(new string[] { "videoRenderer", "reelItemRenderer" }, out JsonObject render))
				{
					items.Add(new  StreamItemRendererExtractor(render));
				} else if (content.TryGetObject("shortsLockupViewModel", out var shortsLockupViewModel))
				{
					items.Add(new StreamItemShortsLockupViewModelExtractor(shortsLockupViewModel));
				} else if (content.TryGetObject("lockupViewModel", out var lockupViewModel))
				{
					items.Add(new StreamItemLockupViewModelExtractor(lockupViewModel));
				}
			}
				
			return (title, items);

		}

		/// <summary>
		/// Extracts Stream Info from ItemSectionRenderer
		/// </summary>
		/// <param name="itemSectionRenderer">Item Section Renderer Object</param>
		/// <returns>(title, StreamInfoExtractor[])</returns>
		/// <exception cref="ParsingException"></exception>
		internal static (string, StreamItemRendererExtractor[]) ParseItemSectionRenderer(JsonObject itemSectionRenderer)
		{
			if (itemSectionRenderer.ContainsKey("contents"))
			{
				if (itemSectionRenderer.GetArray("contents").GetObject(0)
						.TryGetOneOf<JsonObject>(new string[] { "shelfRenderer", "reelShelfRenderer" }, out var shelf))
					return ParseShelfRenderer(shelf);
			}

			return default;
		}

		/// <summary>
		/// Extracts Stream Info from ShelfRenderer
		/// </summary>
		/// <param name="shelfRenderer">Shelf Renderer Object</param>
		/// <returns>(title, StreamInfoExtractor[])</returns>
		/// <exception cref="ParsingException"></exception>
		internal static (string, StreamItemRendererExtractor[]) ParseShelfRenderer(JsonObject shelfRenderer)
		{
			StreamItemRendererExtractor[] contents;
			if (shelfRenderer.Has("content"))
			{
				contents = shelfRenderer.GetObject("content")
					.GetOneOf<JsonObject>(new string[] { "horizontalListRenderer", "expandedShelfContentsRenderer" })
					.GetArray("items")
					.Select(i => new StreamItemRendererExtractor(i.GetOneOf<JsonObject>(new string[] { "gridVideoRenderer", "videoRenderer" })))
					.ToArray();
			}
			else
			{
				contents = shelfRenderer.GetArray("items")
					.Select(i => new StreamItemRendererExtractor(i.GetObject("reelItemRenderer")))
					.ToArray();
			}
			string title = null;
			if (shelfRenderer.TryGetObject("title", out var titleObj)) title = GetText(titleObj);
			return (title, contents);
		}

		/// <summary>
		/// Extracts Continuation token from ContinuationItemRenderer
		/// </summary>
		/// <param name="continuationItemRenderer">Continuation Item Renderer Object</param>
		/// <returns>Continuation token</returns>
		/// <exception cref="ParsingException"></exception>
		internal static string ParseContinuationItemRenderer(JsonObject continuationItemRenderer)
		{
			return continuationItemRenderer.Get<string>("continuationEndpoint.continuationCommand.token");
		}

		internal static string GetText(JsonObject obj)
		{
			string text;
			if (obj.Has("runs"))
				text = obj.GetArray("runs").GetObject(0).Get<string>("text");
			else obj.TryGet("simpleText", out text);

			return text;
		}

		internal static ChannelBadgeType ParseChannelBadgeType(JsonArray badges)
		{
			var badge = badges.FirstOrDefault(b => b.TryGet<string>("metadataBadgeRenderer.style", out var _));
			if (badge == null || !badge.TryGet<string>("metadataBadgeRenderer.style", out var badgeStyle))
				return ChannelBadgeType.None;
			
			return GetChannelBadgeType(badgeStyle);
		}

		internal static ChannelBadgeType GetChannelBadgeType(string badgeStyle)
		{
			switch (badgeStyle)
			{
				case "BADGE_STYLE_TYPE_VERIFIED":
				case "CHECK_CIRCLE_FILLED":
					return ChannelBadgeType.Verified;
				case "AUDIO_BADGE":
					return ChannelBadgeType.Audio;
				default:
					return ChannelBadgeType.None;
			}
		}

		internal static string GetUrl(JsonObject navigationEndpoint)
		{
			return navigationEndpoint.Get<string>("commandMetadata.webCommandMetadata.url");
		}

		internal static string GetBrowseId(JsonObject navigationEndpoint)
		{
			return navigationEndpoint.Get<string>("browseEndpoint.browseId");
		}
	}
}

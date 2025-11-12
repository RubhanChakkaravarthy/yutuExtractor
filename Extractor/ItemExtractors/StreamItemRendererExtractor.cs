using System;
using System.Linq;
using System.Text.Json.Nodes;
using Extractor.ItemExtractors.Interface;
using Extractor.Models;
using Extractor.Models.Enums;
using Extractor.Utilities;

namespace Extractor.ItemExtractors
{
	internal class StreamItemRendererExtractor : IStreamItemExtractor
	{
		private readonly JsonObject _streamItemObject;

		public StreamItemRendererExtractor(JsonObject streamItemObject)
		{
			_streamItemObject = streamItemObject;
		}

		public string GetName()
		{
			return _streamItemObject.TryGetOneOf(new [] { "title", "headline" }, out JsonObject nameObj) 
				? ParsingHelpers.GetText(nameObj) 
				: null;
		}

		public string GetUrl()
		{
			return ParsingHelpers.GetUrl(_streamItemObject.GetObject("navigationEndpoint"));
		}

		public ThumbnailInfo[] GetThumbnails()
		{
			return ParsingHelpers.ParseThumbnails(_streamItemObject.GetArray("thumbnail.thumbnails"));
		}

		public ItemType GetItemType() => ItemType.STREAM;

		public string GetVideoId()
		{
			return _streamItemObject.Get<string>("videoId");
		}

		public string GetDescription()
		{
			return _streamItemObject.TryGetObject("descriptionSnippet", out var descriptionObj) 
				? ParsingHelpers.GetText(descriptionObj) 
				: null;
		}

		public TimeSpan GetDuration()
		{
			string durationText = null;
			if (_streamItemObject.Has("lengthText"))
			{
				durationText = _streamItemObject.Get<string>("lengthText.simpleText");
			}
			else if (_streamItemObject.Has("thumbnailOverlays"))
			{
				durationText = _streamItemObject.GetArray("thumbnailOverlays")
					.FirstOrDefault(t => t.Has("thumbnailOverlayTimeStatusRenderer"))
					?.Get<string>("thumbnailOverlayTimeStatusRenderer.text.simpleText");
			}

			if (!string.IsNullOrWhiteSpace(durationText))
			{
				var durations = durationText.Split(':')
				.Reverse()
				.Select(int.Parse).ToArray();

				return new TimeSpan(durations.ElementAtOrDefault(2), durations.ElementAtOrDefault(1), durations.ElementAtOrDefault(0));
			}

			return default;
		}

		public string GetPublishedTime()
		{
			string publishedTime = null;
			if (_streamItemObject.Has("publishedTimeText"))
				publishedTime = _streamItemObject.Get<string>("publishedTimeText.simpleText");
			else if (_streamItemObject.Has("videoInfo"))
				publishedTime = _streamItemObject.GetArray("videoInfo.runs").Get<string>(2);
			return publishedTime;
		}

		public string GetViewCount()
		{
			string viewCount = null;
			if (_streamItemObject.TryGetObject("viewCountText", out var viewCountObj) || _streamItemObject.TryGetObject("videoInfo", out viewCountObj))
			{
				// viewCount = viewCountObj.Has("simpleText")
				// 	? viewCountObj.Get<string>("simpleText")
				// 	: viewCountObj.GetArray("runs").Aggregate(string.Empty, (current, next) => current + " " + next.Get<string>("text"));
				viewCount = ParsingHelpers.GetText(viewCountObj);
			}
			return viewCount;
		}

		public ChannelItem GetUploader()
		{
			if (_streamItemObject.TryGetOneOf<JsonObject>(new [] { "ownerText", "shortByLineText", "longBylineText" }, out var uploaderData))
				uploaderData = uploaderData.GetArray("runs").GetObject(0);
			if (uploaderData != null)
			{
				var channel = new ChannelItem
				{
					Name = uploaderData.Get<string>("text"),
					Url = ParsingHelpers.GetUrl(uploaderData.GetObject("navigationEndpoint")),
					ChannelId = ParsingHelpers.GetBrowseId(uploaderData.GetObject("navigationEndpoint")),
					ItemType = ItemType.CHANNEL
				};

				if (_streamItemObject.Has("ownerBadges"))
				{
					channel.BadgeType = ParsingHelpers.ParseChannelBadgeType(_streamItemObject.GetArray("ownerBadges"));
				}

				if (_streamItemObject.Has("channelThumbnailSupportedRenderers"))
				{
					channel.Thumbnails = ParsingHelpers.ParseThumbnails(
						_streamItemObject.GetArray("channelThumbnailSupportedRenderers.channelThumbnailWithLinkRenderer.thumbnail.thumbnails"));
				}
				else if (_streamItemObject.Has("channelThumbnail"))
				{
					channel.Thumbnails = ParsingHelpers.ParseThumbnails(_streamItemObject.GetArray("channelThumbnail.thumbnails"));
				}

				return channel;
			}

			return null;
		}

		public StreamType GetStreamType()
		{
			if (_streamItemObject.TryGetArray("badges", out var badges) && badges
					.FirstOrDefault(b => b.Get<string>("metadataBadgeRenderer.style") == "BADGE_STYLE_TYPE_LIVE_NOW") != null)
				return StreamType.LIVE_STREAM;
			else
				return StreamType.VIDEO_STREAM;
		}

		public bool IsReel()
		{
			bool isReel = _streamItemObject.Has("videoType") && _streamItemObject.Get<string>("videoType") == "REEL_VIDEO_TYPE_VIDEO";

			if (!isReel)
				isReel = _streamItemObject.Has("navigationEndpoint.reelWatchEndpoint");

			return isReel;
		}
	}
}

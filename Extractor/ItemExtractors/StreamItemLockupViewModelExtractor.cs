using System;
using System.Linq;
using System.Text.Json.Nodes;
using Extractor.ItemExtractors.Interface;
using Extractor.Models;
using Extractor.Models.Enums;
using Extractor.Utilities;

namespace Extractor.ItemExtractors
{
	public class StreamItemLockupViewModelExtractor : IStreamItemExtractor
	{
		private readonly JsonObject _lockupViewModel;
		
		public StreamItemLockupViewModelExtractor(JsonObject lockupViewModel)
		{
			_lockupViewModel = lockupViewModel;
		}
		
		public string GetName()
		{
			return _lockupViewModel.Get<string>("metadata.lockupMetadataViewModel.title.content");
		}

		public string GetUrl()
		{
			return ParsingHelpers.GetUrl(_lockupViewModel.GetObject("itemPlayback.inlinePlayerData.onSelect.innertubeCommand"));
		}

		public ThumbnailInfo[] GetThumbnails()
		{
			return ParsingHelpers.ParseThumbnails(_lockupViewModel.GetArray("contentImage.thumbnailViewModel.image.sources"));
		}

		public ItemType GetItemType() => ItemType.STREAM;

		public string GetVideoId()
		{
			return _lockupViewModel.Get<string>("contentId");
		}

		public string GetDescription()
		{
			return null;
		}

		public TimeSpan GetDuration()
		{
			var thumbnailOverlayText = GetThumbnailOverlayText();

			if (thumbnailOverlayText == "LIVE") return TimeSpan.Zero;
			
			var duration = thumbnailOverlayText.Split(':')
				.Reverse()
				.Select(int.Parse)
				.ToList();
			return new TimeSpan(duration.ElementAtOrDefault(3), duration.ElementAtOrDefault(2), duration.ElementAtOrDefault(1), duration.ElementAtOrDefault(0));
		}
		
		public string GetPublishedTime()
		{
			if (GetMetaRows() .GetObject(1) 
			    .GetArray("metadataParts")
			    .TryGetObject(1, out var publishedTime))
			{
				return publishedTime
					.Get<string>("text.content");
			}

			return null;
		}

		public string GetViewCount()
		{
			return GetMetaRows()
				.GetObject(1)
				.GetArray("metadataParts")
				.GetObject(0)
				.Get<string>("text.content");
		}

		public ChannelItem GetUploader()
		{
			var uploaderData = GetMetaRows()
				.GetObject(0)
				.GetArray("metadataParts")
				.GetObject(0)
				.GetObject("text");
			
			var channel = new ChannelItem
			{
				Name = uploaderData.Get<string>("content"),
				ItemType = ItemType.CHANNEL,
				OnLive = _lockupViewModel.TryGet<string>("metadata.lockupMetadataViewModel.image.decoratedAvatarViewModel.liveData.liveBadgeText", out var liveBadgeText) 
				         && liveBadgeText == "LIVE"
			};
			
			// Not available for videos with multi-uploader (collaborators)
			if (uploaderData.TryGetArray("commandRuns", out var commandRuns))
			{
				var innertubeCommandObj =
					commandRuns.GetObject(0).GetObject("onTap.innertubeCommand");
				channel.ChannelId = ParsingHelpers.GetBrowseId(innertubeCommandObj);
				channel.Url = ParsingHelpers.GetUrl(innertubeCommandObj);
			}

			if (uploaderData.TryGetArray("attachmentRuns", out var attachmentRuns))
			{
				channel.BadgeType = ParsingHelpers.GetChannelBadgeType(attachmentRuns
					.GetObject(0)
					.GetArray("element.type.imageType.image.sources")
					.GetObject(0)
					.Get<string>("clientResource.imageName"));
			}

			if (_lockupViewModel.TryGetArray(
				    "metadata.lockupMetadataViewModel.image.decoratedAvatarViewModel", out var avatar))
			{
				channel.Thumbnails = ParsingHelpers.ParseThumbnails(avatar.GetArray("avatar.avatarViewModel.image.source"));
			} else if (_lockupViewModel.TryGetArray("metadata.lockupMetadataViewModel.image.avatarStackViewModel.avatars", out var avatars))
			{
				// Picking the first channel thumbnails temporarily till properly support multichannel
				channel.Thumbnails = ParsingHelpers.ParseThumbnails(avatars.GetObject(0).GetArray("avatarViewModel.image.sources"));
			}

			return channel;
		}

		public StreamType GetStreamType()
		{
			return GetThumbnailOverlayText() == "LIVE" ? StreamType.LIVE_STREAM : StreamType.VIDEO_STREAM;
		}

		public bool IsReel() => false;

		private string GetThumbnailOverlayText()
		{
			return _lockupViewModel.GetArray("contentImage.thumbnailViewModel.overlays")
				.GetObject(0)
				.GetArray("thumbnailOverlayBadgeViewModel.thumbnailBadges")
				.GetObject(0)
				.Get<string>("thumbnailBadgeViewModel.text");
		}

		private JsonArray GetMetaRows()
		{
			return _lockupViewModel
				.GetArray("metadata.lockupMetadataViewModel.metadata.contentMetadataViewModel.metadataRows");
		}
	}
}
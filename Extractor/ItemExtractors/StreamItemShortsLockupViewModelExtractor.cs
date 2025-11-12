using System;
using System.Text.Json.Nodes;
using Extractor.ItemExtractors.Interface;
using Extractor.Models;
using Extractor.Models.Enums;
using Extractor.Utilities;

namespace Extractor.ItemExtractors
{
	public class StreamItemShortsLockupViewModelExtractor : IStreamItemExtractor
	{
		private readonly JsonObject _shortsLockupViewModel;
		
		public StreamItemShortsLockupViewModelExtractor(JsonObject shortsLockupViewModel)
		{
			_shortsLockupViewModel = shortsLockupViewModel;
		}
		
		public string GetName()
		{
			return _shortsLockupViewModel.Get<string>("overlayMetadata.primaryText.content");
		}

		public string GetUrl()
		{
			return ParsingHelpers.GetUrl(_shortsLockupViewModel.GetObject("onTap.innertubeCommand"));
		}

		public ThumbnailInfo[] GetThumbnails()
		{
			return ParsingHelpers.ParseThumbnails(_shortsLockupViewModel.GetArray("thumbnail.sources"));
		}

		public ItemType GetItemType() => ItemType.STREAM;

		public string GetVideoId()
		{
			return _shortsLockupViewModel.Get<string>("onTap.innertubeCommand.reelWatchEndpoint.videoId");
		}

		public string GetDescription()
		{
			return null;
		}

		public TimeSpan GetDuration()
		{
			return TimeSpan.Zero;
		}
		
		public string GetPublishedTime()
		{
			return null;
		}

		public string GetViewCount()
		{
			return _shortsLockupViewModel.Get<string>("overlayMetadata.secondaryText.content");
		}

		public ChannelItem GetUploader()
		{
			return null;
		}

		public StreamType GetStreamType()
		{
			return StreamType.VIDEO_STREAM;
		}

		public bool IsReel() => true;
	}
}
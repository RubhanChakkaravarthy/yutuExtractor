using System.Text.Json.Nodes;
using Extractor.ItemExtractors.Interface;
using Extractor.Models;
using Extractor.Models.Enums;
using Extractor.Utilities;

namespace Extractor.ItemExtractors
{
	internal class PlaylistItemExtractor : IItemExtractor<PlaylistItem>
	{
		private readonly JsonObject _playlistItemObject;

		public PlaylistItemExtractor(JsonObject playlistItemObject)
		{
			_playlistItemObject = playlistItemObject;
		}

		public ItemType GetItemType()
		{
			return ItemType.PLAYLIST;
		}

		public string GetName()
		{
			return _playlistItemObject.Get<string>("title.simpleText");
		}

		public ThumbnailInfo[] GetThumbnails()
		{
			return ParsingHelpers.ParseThumbnails(_playlistItemObject.GetArray("thumbnails").GetObject(0).GetArray("thumbnails"));
		}

		public string GetUrl()
		{
			return ParsingHelpers.GetUrl(_playlistItemObject.GetArray("viewPlaylistText.runs").GetObject(0).GetObject("navigationEndpoint"));
				
		}

		public string GetPlaylistId()
		{
			return _playlistItemObject.Get<string>("playlistId");
		}

		public ChannelItem GetUploader()
		{
			var uploaderData = _playlistItemObject.GetOneOf<JsonObject>(new [] { "shortByLineText", "longBylineText" }).GetArray("runs").GetObject(0);
			return new ChannelItem {
				Name = uploaderData.Get<string>("text"),
				Url = ParsingHelpers.GetUrl(uploaderData.GetObject("navigationEndpoint")),
				ChannelId = ParsingHelpers.GetBrowseId(uploaderData.GetObject("navigationEndpoint")),
				ItemType = ItemType.CHANNEL
			};
		}

		public int GetVideosCount()
		{
			if (_playlistItemObject.TryGet("videoCount", out string videoCountStr) && int.TryParse(videoCountStr, out int videoCount))
				return videoCount;
			else if (_playlistItemObject.TryGetArray("videoCountText", out var videoCountArr))
			{
				videoCountStr = videoCountArr.GetArray(0).Get<string>("text");
				if (int.TryParse(videoCountStr, out videoCount)) return videoCount;
			}
			return -1;
		}
	}
}
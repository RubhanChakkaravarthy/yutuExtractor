using System.Text.Json.Nodes;
using Extractor.ItemExtractors.Interface;
using Extractor.Models;
using Extractor.Models.Enums;
using Extractor.Utilities;

namespace Extractor.ItemExtractors
{
    public class ChannelItemExtractor : IItemExtractor<ChannelItem>
    {
        private readonly JsonObject _channelItemObject;

        public ChannelItemExtractor(JsonObject channelItemObject)
        {
            _channelItemObject = channelItemObject;
        }

        public ItemType GetItemType()
        {
            return ItemType.CHANNEL;
        }

        public string GetName()
        {
            return ParsingHelpers.GetText(_channelItemObject
                .GetOneOf<JsonObject>(new [] { "shortByLineText", "longBylineText" }));
        }

        public ThumbnailInfo[] GetThumbnails()
        {
            return ParsingHelpers.ParseThumbnails(_channelItemObject.GetArray("thumbnail.thumbnails"));
        }

        public string GetUrl()
        {
            return ParsingHelpers.GetUrl(_channelItemObject
                .GetObject("navigationEndpoint"));
        }

        public string GetChannelId()
        {
            if (!_channelItemObject.TryGet("channelId", out string channelId))
                channelId = ParsingHelpers.GetBrowseId(_channelItemObject.GetObject("navigationEndpoint"));
            return channelId;
        }

        public ChannelBadgeType GetBadgeType()
        {
	        return _channelItemObject.TryGetArray("ownerBadges", out var badges) ? ParsingHelpers.ParseChannelBadgeType(badges) : ChannelBadgeType.None;
        }

        public string GetSubscribersCount()
        {
            return ParsingHelpers.GetText(_channelItemObject.GetObject("videoCountText"));
        }
    }
}
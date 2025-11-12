using System;
using Extractor.ItemExtractors;
using Extractor.Models;

namespace Extractor.ItemCollectors
{
	internal class ChannelItemCollector : ItemCollector<ChannelItem, ChannelItemExtractor>
	{
		protected override ChannelItem Extract(ChannelItemExtractor extractor)
		{
			var item = new ChannelItem
			{
				Name = extractor.GetName(),
				Url = extractor.GetUrl(),
                Thumbnails = extractor.GetThumbnails(),
				ItemType = extractor.GetItemType()
			};

			try
			{
				item.ChannelId = extractor.GetChannelId();
			}
			catch (Exception ex)
			{
				AddException(ex);
			}

			try
			{
				item.SubscribersCount = extractor.GetSubscribersCount();
			}
			catch (Exception ex)
			{
				AddException(ex);
			}

			try
			{
				item.BadgeType = extractor.GetBadgeType();
			}
			catch (Exception ex)
			{
				AddException(ex);
			}

			return item;
		}
	}
}
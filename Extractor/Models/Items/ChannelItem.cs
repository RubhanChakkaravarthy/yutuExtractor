using Extractor.Models.Enums;

namespace Extractor.Models
{
	public class ChannelItem : Item
	{
		public string ChannelId { get; set; }
		public ChannelBadgeType BadgeType { get; set; }
		public string SubscribersCount { get; set; }
		public bool OnLive { get; set; }

		public override string ToString()
		{
			return $"{Name} {(BadgeType != ChannelBadgeType.None ? "\u2713" : "")}\t{SubscribersCount}\n{Url}";
		}
	}
}
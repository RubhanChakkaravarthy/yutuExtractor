using System;
using Extractor.Models;
using Extractor.Models.Enums;

namespace Extractor.ItemExtractors.Interface
{
	public interface IStreamItemExtractor : IItemExtractor<StreamItem>
	{ 
		string GetVideoId();
		string GetDescription();
		TimeSpan GetDuration();
		string GetPublishedTime();
		string GetViewCount();
		ChannelItem GetUploader();
		StreamType GetStreamType();
		bool IsReel();
	}
}
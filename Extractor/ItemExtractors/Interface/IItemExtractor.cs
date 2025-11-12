using Extractor.Models;
using Extractor.Models.Enums;

namespace Extractor.ItemExtractors.Interface
{
	public interface IItemExtractor<T>
	{
		string GetName();
		string GetUrl();
		ThumbnailInfo[] GetThumbnails();
		ItemType GetItemType();
	}
}

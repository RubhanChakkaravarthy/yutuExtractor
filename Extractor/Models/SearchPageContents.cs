using System.Collections.Generic;

namespace Extractor.Models
{
    public class SearchPageContents : PageContents
    {
        public string SearchText { get; set; }
        public List<Item> Items { get; set; }
        public List<ItemsSection<StreamItem>> Sections { get; set; }
    }
}
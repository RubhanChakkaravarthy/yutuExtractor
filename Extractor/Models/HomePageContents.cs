using System.Collections.Generic;

namespace Extractor.Models
{
    public class HomePageContents : PageContents
    {
        public List<StreamItem> Items { get; set; }
        public List<ItemsSection<StreamItem>> Sections { get; set; }
    }
}
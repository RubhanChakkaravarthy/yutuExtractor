using System.Collections.Generic;

namespace Extractor.Models
{
    public class ItemsSection<T>
    {
        public string Name { get; set; }

        public List<T> Items { get; set; }

        public List<System.Exception> Exceptions { get; set; }

        public string ContinuationToken { get; set; }
    }
}
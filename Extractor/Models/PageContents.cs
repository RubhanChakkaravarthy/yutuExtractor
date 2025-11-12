using System;
using System.Collections.Generic;

namespace Extractor.Models
{
	public class PageContents
	{
		public List<Exception> Exceptions { get; set; }
		public string ContinuationToken { get; set; }
		public string VisitorId { get; set; }
	}
}
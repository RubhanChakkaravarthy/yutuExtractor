﻿using System;

namespace Extractor.Exceptions
{
	/// <summary>
	/// Parsing Exception
	/// </summary>
	public class ParsingException : Exception
	{
		public ParsingException(string message) : base(message) {}

		public ParsingException(string message, Exception inner) : base(message, inner) {}
	}
}

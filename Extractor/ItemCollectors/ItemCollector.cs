using System;
using System.Collections.Generic;
using Extractor.ItemExtractors.Interface;
using Extractor.Models;

namespace Extractor.ItemCollectors
{
	internal abstract class ItemCollector<T, E> where T: Item where E : IItemExtractor<T>
	{
		private readonly List<T> _items = new List<T>();
		private readonly List<Exception> _exceptions = new List<Exception>();

		public List<T> Items
		{
			get => _items;
		}

		public List<Exception> Exceptions
		{
			get => _exceptions;
		}

		protected void AddItem(T item)
		{
			_items.Add(item);
		}

		protected void AddException(Exception exception)
		{
			_exceptions.Add(exception);
		}

		public void Reset()
		{
			_items.Clear();
			_exceptions.Clear();
		}

		protected abstract T Extract(E extractor);

		public void Collect(IEnumerable<E> itemExtractors)
		{
			foreach (var itemExtractor in itemExtractors)
			{
				Collect(itemExtractor);
			}
		}

		public void Collect(E itemExtractor)
		{
			try
			{
				AddItem(Extract(itemExtractor));
			}
			catch (Exception ex)
			{
				AddException(ex);
			}
		}
	}
}

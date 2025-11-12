using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Extractor.Exceptions;
using Extractor.ItemCollectors;
using Extractor.Models;
using Extractor.Utilities;

namespace Extractor.PageExtractors
{
    public class TrendingPageExtractor : PageExtractor
    {
        private static readonly string s_browseId = "FEtrending";
        private static readonly string s_endpoint = "browse";
        private static readonly Dictionary<string, string> s_params = new Dictionary<string, string>();

        public TrendingPageExtractor(HttpClient client, CultureInfo culture) : base(client, culture) { }
        public TrendingPageExtractor(HttpClient httpClient) : base(httpClient) { }

        /// <summary>
		/// Fetch Initial Page Data
		/// </summary>
		/// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException"></exception>
        private async Task<JsonObject> FetchDataAsync(string name)
        {
            if (!s_params.TryGetValue(name, out var param))
            {
                param = string.Empty;
            }

            return await FetchContentsAsync(s_endpoint,
	            new Dictionary<string, JsonNode> { { "browseId", s_browseId }, { "params", param } });
        }

        /// <summary>
        /// Get Trending Page Contents
        /// </summary>
        /// <param name="name">tab name</param>
        /// <returns>Trending page contents of the tab</returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException"></exception>
        /// <exception cref="ParsingException"></exception>
        public async Task<TrendingPageContents> GetContentsAsync(string name = "")
        {
            var response = await FetchDataAsync(name);
            var titlesAndParams = GetTitlesAndParams(response);
            var selectedTabName = GetSelectedTabName(GetTabs(response));

            var pageContents = new TrendingPageContents {
                Name = selectedTabName,
                Titles = new List<string>()
            };

            foreach (var (title, param) in titlesAndParams)
            {
                s_params[title] = param;
                pageContents.Titles.Add(title);
            }

            var tab = GetTab(selectedTabName, response);
            var itemsSections = tab.GetArray("tabRenderer.content.sectionListRenderer.contents")
                .Select(c => ParsingHelpers.ParseItemSectionRenderer(c.GetObject("itemSectionRenderer")))
                .ToList();

            var mainItemsCollector = new StreamItemCollector();
            mainItemsCollector.Collect(itemsSections.Where(i => i.Item1 == null).SelectMany(i => i.Item2));

            pageContents.Items = mainItemsCollector.Items;
            pageContents.Exceptions = mainItemsCollector.Exceptions;

            try
            {
                var additionalSections = itemsSections.Where(i => i.Item1 != null).ToList();
                if (additionalSections.Any())
                {
                    pageContents.Sections = new List<ItemsSection<StreamItem>>();
                    foreach (var (title, streamExtractors) in additionalSections)
                    {
                        var collector = new StreamItemCollector();
                        collector.Collect(streamExtractors);
                        pageContents.Sections.Add(new ItemsSection<StreamItem> {
                            Name = title,
                            Items = collector.Items,
                            Exceptions = collector.Exceptions
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                pageContents.Exceptions.Add(new ParsingException($"Unable to get \"Sections\" in {pageContents.Name} tab of trending page", ex));
            }

            return pageContents;
        }

        private static JsonArray GetTabs(JsonObject data)
        {
            return data.GetArray("contents.twoColumnBrowseResultsRenderer.tabs");
        }

        private static string GetSelectedTabName(JsonArray tabs)
        {
            try
            {
                return tabs
                    .First(t => t.Get<bool>("tabRenderer.selected"))
                    .Get<string>("tabRenderer.title");
            }
            catch (Exception ex)
            {
                throw new ParsingException("Unable to get \"Selected Tabname\"", ex);
            }

        }

        private static IEnumerable<(string, string)> GetTitlesAndParams(JsonObject data)
        {
            try
            {
                return GetTabs(data)
                    .Select(t => {
                        if (!t.TryGet("tabRenderer.endpoint.browseEndpoint.params", out string param))
                            param = string.Empty;
                        return (t.Get<string>("tabRenderer.title"), param);
                    });
            }
            catch (Exception ex)
            {
                throw new ParsingException("Unable to get \"Titles and Params\" for tabs", ex);
            }
        }

        private static JsonObject GetTab(string name, JsonObject data)
        {
            try
            {
                return GetTabs(data)
                    .First(t => t.Get<string>("tabRenderer.title") == name)
                    .AsObject();

            }
            catch (Exception ex) when (ex is NullReferenceException || ex is InvalidOperationException)
            {
                throw new ParsingException($"Unable to get \"{name}\" tabs", ex);
            }
        }
    }
}
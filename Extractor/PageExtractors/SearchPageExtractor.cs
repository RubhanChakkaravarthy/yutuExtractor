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
using Extractor.ItemExtractors;
using Extractor.Models;
using Extractor.Utilities;

namespace Extractor.PageExtractors
{
    public class SearchPageExtractor : PageExtractor
    {
        private static readonly string s_endpoint = "search";

        public SearchPageExtractor(HttpClient client) : base(client) { }
        public SearchPageExtractor(HttpClient client, CultureInfo culture) : base(client, culture) { }

        /// <summary>
        /// Get Search Page Contents
        /// </summary>
        /// <param name="searchText">search text</param>
        /// <returns>Search page contents</returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="JsonException"></exception>
        /// <exception cref="ParsingException"></exception>
        public async Task<SearchPageContents> GetContentsAsync(string searchText)
        {
            var response = await FetchContentsAsync(s_endpoint, new Dictionary<string, JsonNode> { { "query", searchText } });
            var searchContents = GetSearchContents(response);
            return GetPageContents(GetSearchQuery(response), searchContents);
        }

        /// <summary>
        /// Get Search Page Continuation Contents
        /// </summary>
        /// <param name="continuationToken">Continuation Token</param>
        /// <returns>Search page continuation contents</returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException"></exception>
        /// <exception cref="ParsingException"></exception>
        public async Task<SearchPageContents> GetContinuationContentsAsync(string continuationToken)
        {
            var response = await FetchContinuationDataAsync(s_endpoint, continuationToken);
            var searchContents = GetSearchContents(response);
            return GetPageContents(GetSearchQuery(response), searchContents);
        }

        private static SearchPageContents GetPageContents(string searchText, JsonArray searchContents)
        {
            var contents = searchContents.GetObject(0).GetArray("itemSectionRenderer.contents");

            var streamCollector = new StreamItemCollector();
            var playlistCollector = new PlaylistItemCollector();
            var channelCollector = new ChannelItemCollector();

            var pageContents = new SearchPageContents {
                SearchText = searchText,
                Items = new List<Item>(),
                Exceptions = new List<Exception>(),
                ContinuationToken = ParsingHelpers.ParseContinuationItemRenderer(searchContents.GetObject(1).GetObject("continuationItemRenderer"))
            };

            foreach (var content in contents)
            {
                if (content.Has("videoRenderer"))
                    streamCollector.Collect(new StreamItemRendererExtractor(content.GetObject("videoRenderer")));
                else if (content.Has("playlistRenderer"))
                    playlistCollector.Collect(new PlaylistItemExtractor(content.GetObject("playlistRenderer")));
                else if (content.Has("channelRenderer"))
                    channelCollector.Collect(new ChannelItemExtractor(content.GetObject("channelRenderer")));
            }

            pageContents.Items.AddRange(streamCollector.Items);
            pageContents.Items.AddRange(playlistCollector.Items);
            pageContents.Items.AddRange(channelCollector.Items);

            pageContents.Exceptions.AddRange(streamCollector.Exceptions);
            pageContents.Exceptions.AddRange(playlistCollector.Exceptions);
            pageContents.Exceptions.AddRange(channelCollector.Exceptions);

            try
            {
                var sections = contents
                    .Where(c => c.Has("reelShelfRenderer"))
                    .Select(c => ParsingHelpers.ParseShelfRenderer(c.GetObject("reelShelfRenderer")));

                if (sections.Count() > 0)
                {
                    pageContents.Sections = new List<ItemsSection<StreamItem>>();
                    foreach (var (title, items) in sections)
                    {
                        var collector = new StreamItemCollector();
                        collector.Collect(items);
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
                new ParsingException("Unable to get \"Sections\" of Search page", ex);
            }

            return pageContents;
        }

        private JsonArray GetSearchContents(JsonObject data)
        {
            if (data.Has("contents"))
                return data.GetArray("contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.contents");
            else return data.GetArray("onResponseReceivedCommands").GetObject(0).GetArray("appendContinuationItemsAction.continuationItems");
        }

        private string GetSearchQuery(JsonObject response)
        {
            try
            {
                var groups = response.GetArray("header.searchHeaderRenderer.searchFilterButton.buttonRenderer.command.openPopupAction.popup.searchFilterOptionsDialogRenderer.groups");
                foreach (var group in groups)
                {
                    var filters = group.GetArray("searchFilterGroupRenderer.filters");
                    foreach (var filter in filters)
                    {
                        if (filter.TryGet<string>("searchFilterRenderer.navigationEndpoint.searchEndpoint.query", out var searchText))
                            return searchText;
                    }
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Extractor.Exceptions;
using Extractor.Utilities;

namespace Extractor
{
    public class SearchSuggestionExtractor
    {
        private readonly HttpClient _client;
        private readonly CultureInfo _culture;

        public SearchSuggestionExtractor(HttpClient client, CultureInfo culture = null)
        {
            _client = client;
            _culture = culture ?? Thread.CurrentThread.CurrentCulture;
        }

        /// <summary>
		/// Fetch Suggestion Data
		/// </summary>
		/// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException"></exception>
        private async Task<JsonArray> FetchDataAsync(string searchText)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, RequestHelpers.GetSearchCompletionUri(searchText, _culture))) {
                request.AddYoutubeV1Headers(null);
                
                using (var response = await _client.SendAsync(request)) {            
                    response.EnsureSuccessStatusCode();
                    
                    if (!response.Content.Headers.TryGetValues("Content-Type", out var contentType) || 
                        contentType.FirstOrDefault(c => c.Contains("application/json")) == null)
                        throw new ParsingException($"Invalid Response Type got \"{string.Join(";", contentType)}\" Expected Json response");
                    return await response.Content.DeserializeAsync<JsonArray>();
                }
            }
        }

        /// <summary>
		/// Get Search Suggestions
		/// </summary>
        /// <returns>Search Suggestions</returns>
		/// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException"></exception>
        public async Task<string[]> SearchAsync(string searchText)
        {
            var suggestions = await FetchDataAsync(searchText);
            return suggestions.GetArray(1)
                .Select(s => s.AsArray().Get<string>(0))
                .ToArray();
        }
    }
}
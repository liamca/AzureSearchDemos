using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Microsoft.Azure.Search.Models;
using BuildSessions.ViewModels;
using Microsoft.Azure.Search;
using System.Globalization;

namespace BuildSessions.DataModel
{
    public class SearchService
    {
        private const string ServiceName = [Search Service Name];
        private const string QueryKey = [Search Service API Key];
        private const string IndexName = "adventureworks";

        private static readonly SearchIndexClient _indexClient = new SearchIndexClient(ServiceName, IndexName, new SearchCredentials(QueryKey));

        public static async Task<IEnumerable<ItemViewModel>> SearchAsync(string searchString)
        {
            var searchParameters = new SearchParameters()
            {
                Top = 20
            };
            return await DoSearchAsync(searchString, searchParameters);
        }

        public static async Task<IEnumerable<ItemViewModel>> SearchComingNextAsync()
        {
            var searchParameters = new SearchParameters()
            {
                Filter = string.Format(CultureInfo.InvariantCulture,
                                       "Start ge {0:O} and Start lt {1:O}",
                                       DateTime.UtcNow,
                                       DateTime.UtcNow.AddHours(1.5)),
                Top = 20
            };

            return await DoSearchAsync("*", searchParameters);
        }

        private static async Task<IEnumerable<ItemViewModel>> DoSearchAsync(string searchString, SearchParameters parameters)
        {
            List<ItemViewModel> searchResults = new List<ItemViewModel>();
            var response = await _indexClient.Documents.SearchAsync<ItemViewModel>(searchString, parameters);
            searchResults.AddRange(response.Results.Select(result => result.Document));

            return searchResults;
        }
    }
}

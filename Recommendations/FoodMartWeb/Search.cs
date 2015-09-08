using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace FoodMartWeb
{
    public class Search
    {
        private static SearchServiceClient _searchClient;
        private static SearchIndexClient _indexClient;
        private static string IndexName = "foodmart";
        public static string errorMessage;

        static Search()
        {
            try
            {
                string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
                string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];

                // Create an HTTP reference to the catalog index
                _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
                _indexClient = _searchClient.Indexes.GetClient(IndexName);
                
            }
            catch (Exception e)
            {
                errorMessage = e.Message.ToString();
            }
        }

        public DocumentSearchResponse SearchIndex(string searchText, string departmentFacet, string categoryFacet, string priceFacet, string lowFatFacet,
            string sortType, int currentPage)
        {
            // Execute search based on query string
            try
            {
                SearchParameters sp = new SearchParameters()
                {
                    SearchMode = SearchMode.Any,
                    Top = 10,
                    Skip = currentPage-1,
                    // Limit results
                    Select = new List<String>() {"product_id","brand_name","product_name","srp","net_weight","recyclable_package",
                        "low_fat","units_per_case","product_subcategory","product_category","product_department","url", "recommendations"},
                    // Add count
                    IncludeTotalResultCount = true,
                    // Add facets
                    Facets = new List<String>() { "low_fat", "product_category", "product_department", "srp,interval:1" },
                };

                // Define the sort type
                if (sortType == "priceDesc")
                    sp.OrderBy = new List<String>() { "srp desc" };
                else if (sortType == "priceIncr")
                    sp.OrderBy = new List<String>() { "srp" };
                

                // Add filtering
                string filter = null;
                if (categoryFacet != "")
                    filter = "product_category eq '" + categoryFacet + "'";
                if (departmentFacet != "")
                {
                    if (filter != null)
                        filter += " and ";
                    filter += "product_department eq '" + departmentFacet + "'";
                }

                if (lowFatFacet != "")
                {
                    if (filter != null)
                        filter += " and ";
                    filter += "low_fat eq '" + lowFatFacet + "'";
                }

                if (priceFacet != "")
                {
                    if (filter != null)
                        filter += " and ";
                    filter += "srp ge " + priceFacet+ " and srp lt " + (Convert.ToInt32(priceFacet) + 1).ToString();
                }
                
                sp.Filter = filter;

                return _indexClient.Documents.Search(searchText, sp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }

        public DocumentSearchResponse GetProductIDs(JArray relatedItems)
        {
            // Execute search to find all the related products
            try
            {
                SearchParameters sp = new SearchParameters()
                {
                    SearchMode = SearchMode.Any,
                    // Limit results
                    Select = new List<String>() {"product_id","brand_name","product_name","srp","net_weight","recyclable_package",
                        "low_fat","units_per_case","product_subcategory","product_category","product_department","url", "recommendations"},
                };

                // Filter based on the product ID's
                string productIdFilter = null;
                foreach (var item in relatedItems)
                {
                    productIdFilter += "product_id eq '" + item.ToString() + "' or ";
                }
                productIdFilter = productIdFilter.Substring(0, productIdFilter.Length - 4);
                sp.Filter = productIdFilter;

                return _indexClient.Documents.Search("*", sp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }


        public DocumentSuggestResponse Suggest(string searchText, bool fuzzy)
        {
            // Execute search based on query string
            try
            {
                SuggestParameters sp = new SuggestParameters()
                {
                    UseFuzzyMatching = fuzzy,
                    Top = 10
                };

                return _indexClient.Documents.Suggest(searchText, "sg", sp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }

        public DocumentGetResponse LookUp(string id)
        {
            // Execute geo search based on query string
            try
            {
                return _indexClient.Documents.Get(id);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }

    }
}
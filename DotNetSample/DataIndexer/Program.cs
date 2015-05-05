using Microsoft.Azure;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Spatial;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace DataIndexer
{
    class Program
    {
        private static SearchServiceClient _searchClient;
        private static SearchIndexClient _indexClient;
        private static string searchIndexName = "adventureworks";

        // This Sample shows how to delete, create, upload documents and query an index
        static void Main(string[] args)
        {
            string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
            string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];

            // Create an HTTP reference to the catalog index
            _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
            _indexClient = _searchClient.Indexes.GetClient(searchIndexName);

            Console.WriteLine("{0}", "Deleting index...\n");
            if (DeleteIndex())
            {
                Console.WriteLine("{0}", "Creating index...\n");
                CreateIndex();
                Console.WriteLine("{0}", "Sync documents from Azure SQL...\n");
                SyncDataFromAzureSQL();
            }
            Console.WriteLine("{0}", "Complete.  Press any key to end application...\n");
            Console.ReadKey();
        }

        private static bool DeleteIndex()
        {
            // Delete the index if it exists
            try
            {
                _searchClient.Indexes.Delete(searchIndexName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting index: {0}\r\n", ex.Message.ToString());
                Console.WriteLine("Did you remember to add your SearchServiceName and SearchServiceApiKey to the app.config?\r\n");
                return false;
            }

            return true;
        }

        private static void CreateIndex()
        {
            // Create the Azure Search index based on the included schema
            try
            {
                var definition = new Index()
                {
                    Name = searchIndexName,
                    Fields = new[] 
                    { 
                        new Field("CustomerAddressID",  DataType.String)         { IsKey = true,  IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false,IsRetrievable = true},
                        new Field("Title",              DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                        new Field("FirstName",          DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                        new Field("MiddleName",         DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                        new Field("LastName",           DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                        new Field("Suffix",             DataType.String)         { IsKey = false, IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false,IsRetrievable = true},
                        new Field("CompanyName",        DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                        new Field("SalesPerson",        DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                        new Field("EmailAddress",       DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = false,IsRetrievable = true},
                        new Field("Phone",              DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = false,IsRetrievable = true},
                        new Field("AddressType",        DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                        new Field("AddressLine1",       DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = false,IsRetrievable = true},
                        new Field("AddressLine2",       DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = false,IsRetrievable = true},
                        new Field("City",               DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                        new Field("StateProvince",      DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                        new Field("CountryRegion",      DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                        new Field("PostalCode",         DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = false,IsRetrievable = true},
                        new Field("ProductNames",       DataType.Collection(DataType.String))         { IsSearchable = true, IsFilterable = true, IsFacetable = true },

                    }
                };

                _searchClient.Indexes.Create(definition);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating index: {0}\r\n", ex.Message.ToString());
            }

        }

        private static void SyncDataFromAzureSQL()
        {
            // This will use the Azure Search Indexer to synchronize data from Azure SQL to Azure Search
            Uri _serviceUri = new Uri("https://" + ConfigurationManager.AppSettings["SearchServiceName"] + ".search.windows.net");
            HttpClient _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("api-key", ConfigurationManager.AppSettings["SearchServiceApiKey"]);

            Console.WriteLine("{0}", "Deleting Data Source...\n");
            Uri uri = new Uri(_serviceUri, "datasources/adventureworks-datasource");
            HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Delete, uri);
            if (response.StatusCode != HttpStatusCode.NotFound && response.StatusCode != HttpStatusCode.NoContent)
            {
                Console.WriteLine("Error deleting data source: {0}", response.Content.ReadAsStringAsync().Result);
                return;
            }

            Console.WriteLine("{0}", "Deleting Indexer...\n");
            uri = new Uri(_serviceUri, "indexers/adventureworks-indexer");
            response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Delete, uri);
            if (response.StatusCode != HttpStatusCode.NotFound && response.StatusCode != HttpStatusCode.NoContent)
            {
                Console.WriteLine("Error deleting indexer: {0}", response.Content.ReadAsStringAsync().Result);
                return;
            }

            Console.WriteLine("{0}", "Creating Data Source...\n");
            uri = new Uri(_serviceUri, "datasources/adventureworks-datasource");
            string json = "{ 'name' : 'adventureworks-datasource','description' : 'AdventureWorks Dataset','type' : 'azuresql','credentials' : { 'connectionString' : 'Server=tcp:azs-playground.database.windows.net,1433;Database=AdventureWorks;User ID=reader;Password=EdrERBt3j6mZDP;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;' },'container' : { 'name' : 'CustomerAddressOrders' }} ";
            response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Put, uri, json);
            if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.NoContent)
            {
                Console.WriteLine("Error creating data source: {0}", response.Content.ReadAsStringAsync().Result);
                return;
            }

            Console.WriteLine("{0}", "Creating Indexer...\n");
            uri = new Uri(_serviceUri, "indexers/adventureworks-indexer");
            json = "{ 'name' : 'adventureworks-indexer','description' : 'AdventureWorks data indexer','dataSourceName' : 'adventureworks-datasource','targetIndexName' : 'adventureworks','fieldMappings' : [ { 'sourceFieldName' : 'ProductNames', 'mappingFunction' : { 'name' : 'jsonArrayToStringCollection' } } ],'parameters' : { 'maxFailedItems' : 10, 'maxFailedItemsPerBatch' : 5, 'base64EncodeKeys': false }}";
            response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Put, uri, json);
            if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.NoContent)
            {
                Console.WriteLine("Error creating indexer: {0}", response.Content.ReadAsStringAsync().Result);
                return;
            }

            Console.WriteLine("{0}", "Syncing data...\n");
            uri = new Uri(_serviceUri, "indexers/adventureworks-indexer/run");
            response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Post, uri);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                Console.WriteLine("Error running indexer: {0}", response.Content.ReadAsStringAsync().Result);
                return;
            }

            bool running = true;
            Console.WriteLine("{0}", "Synchronization running...\n");
            while (running)
            {
                uri = new Uri(_serviceUri, "indexers/adventureworks-indexer/status");
                response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Get, uri);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine("Error polling for indexer status: {0}", response.Content.ReadAsStringAsync().Result);
                    return;
                }

                var result = AzureSearchHelper.DeserializeJson<dynamic>(response.Content.ReadAsStringAsync().Result);
                if (result.lastResult != null)
                {
                    switch ((string)result.lastResult.status)
                    {
                        case "inProgress":
                            Console.WriteLine("{0}", "Synchronization running...\n");
                            Thread.Sleep(1000);
                            break;

                        case "success":
                            running = false;
                            Console.WriteLine("Synchronized {0} rows...\n", result.lastResult.itemsProcessed.Value);
                            break;

                        default:
                            running = false;
                            Console.WriteLine("Synchronization failed: {0}\n", result.lastResult.errorMessage);
                            break;
                    }
                }
            }
        }
    }
}

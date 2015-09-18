// This is an Azure Search> demo website based on data from the FoodMart sample database, part of SQL Server Analysis Services 2000.  
// Images are prvided by wikipedia.org.  
// Items listed here should not be considered active or accurate.

using FoodMart;
using Microsoft.Azure;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace FoodMartSetup
{
    class Program
    {
        private static SearchServiceClient _searchClient;
        private static SearchIndexClient _indexClient;
        private static string IndexName = "foodmart";

        private static string zipFile = @"mahout-distribution-0.10.0.zip";
        private static string extractPath = @"c:\dist";
        private static string azureBlobDir = "dist";




        private static string azureBlobStorageName = ConfigurationManager.AppSettings["AzureBlobStorageName"];
        private static string azureBlobStorageKey = ConfigurationManager.AppSettings["AzureBlobStorageKey"];
        private static string azureBlobStorageContainer = ConfigurationManager.AppSettings["AzureBlobStorageContainer"];
        
        private static string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
        private static string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];

        private static string azureSQLServer = ConfigurationManager.AppSettings["AzureSQLServer"];
        private static string azureSQLDatabase = ConfigurationManager.AppSettings["AzureSQLDatabase"];
        private static string azureSQLUser = ConfigurationManager.AppSettings["AzureSQLUser"];
        private static string azureSQLPassword = ConfigurationManager.AppSettings["AzureSQLPassword"];
        

        static void Main(string[] args)
        {
            //Download Mahout distribution and store in Azure Blob 
            DownloadAndUnzipMahoutDistribution();
            UploadMahoutDistributiontoBlobStorage();

            UploadHiveQueryToBlobStorage();

            //Setup the Azure SQL Database
            CreateAzureSQLTables();
            BCPDataIntoAzureSQLTable();

            //Setup the Azure Search Index
            // Create an HTTP reference to the catalog index
            _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
            _indexClient = _searchClient.Indexes.GetClient(IndexName);

            Console.WriteLine("{0}", "Deleting index...\n");
            DeleteIndex();
            Console.WriteLine("{0}", "Creating index...\n");
            CreateIndex();
            Console.WriteLine("{0}", "Uploading documents...\n");
            UploadDocuments("data1.json");
            UploadDocuments("data2.json");
            Console.WriteLine("{0}", "Creating the Azure Search Indexer...\n");
            CreateIndexer();
            Console.WriteLine("{0}", "Complete.  Press any key to end application...\n");
            Console.ReadKey();
        }

        private static bool DeleteIndex()
        {
            try
            {
                AzureOperationResponse response = _searchClient.DataSources.Delete("foodmart-rec-ds");
                response = _searchClient.Indexers.Delete("foodmart-rec-indexer");

                response = _searchClient.Indexes.Delete(IndexName);
                return response.StatusCode != HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting index: {0}:\r\n", ex.Message.ToString());
                return false;
            }

        }

        private static void CreateIndex()
        {
            try
            {
                Suggester sg = new Suggester();
                sg.Name = "sg";
                sg.SearchMode = SuggesterSearchMode.AnalyzingInfixMatching;
                sg.SourceFields = new string[] { "brand_name", "product_name", "sku", "product_subcategory", "product_category", "product_department", "product_family" };

                var definition = new Index()
                {
                    Name = IndexName,
                    Fields = new[] 
                    { 
                        new Field("product_id",         DataType.String)        { IsKey = true,  IsSearchable = false, IsFilterable = true,  IsSortable = false, IsFacetable = false, IsRetrievable = true  },
                        new Field("product_class_id",   DataType.Int32)         { IsKey = false, IsSearchable = false, IsFilterable = false, IsSortable = true,  IsFacetable = false, IsRetrievable = true  },
                        new Field("brand_name",         DataType.String)        { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true  },
                        new Field("product_name",       DataType.String)        { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true  },
                        new Field("sku",                DataType.String)        { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true  },
                        new Field("srp",                DataType.Int32)         { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true,  IsRetrievable = true  },
                        new Field("gross_weight",       DataType.Double)        { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true,  IsRetrievable = true  },
                        new Field("net_weight",         DataType.Double)        { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true,  IsRetrievable = true  },
                        new Field("recyclable_package", DataType.String)        { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = false, IsFacetable = true,  IsRetrievable = true  },
                        new Field("low_fat",            DataType.String)        { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = false, IsFacetable = true,  IsRetrievable = true  },
                        new Field("units_per_case",     DataType.Int32)         { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true,  IsRetrievable = true  },
                        new Field("cases_per_pallet",   DataType.Int32)         { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true,  IsRetrievable = true  },
                        new Field("shelf_width",        DataType.Double)        { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true,  IsRetrievable = true  },
                        new Field("shelf_height",       DataType.Double)        { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true,  IsRetrievable = true  },
                        new Field("shelf_depth",        DataType.Double)        { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true,  IsRetrievable = true  },
                        new Field("product_subcategory",DataType.String)        { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = true,  IsRetrievable = true  },
                        new Field("product_category",   DataType.String)        { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = true,  IsRetrievable = true  },
                        new Field("product_department", DataType.String)        { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = true,  IsRetrievable = true  },
                        new Field("product_family",     DataType.String)        { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = true,  IsRetrievable = true  },
                        new Field("url",                DataType.String)        { IsKey = false, IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true  },
                        new Field("recommendations",    DataType.Collection(DataType.String)) { IsSearchable = true, IsFilterable = true, IsFacetable = true }
                    }
                };
                definition.Suggesters.Add(sg);
                _searchClient.Indexes.Create(definition);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating index: {0}:\r\n", ex.Message.ToString());
            }

        }

        private static void UploadDocuments(string fileName)
        {
            List<IndexAction> indexOperations = new List<IndexAction>();
            DirectoryInfo Folder = new DirectoryInfo(IndexName); ;
            IndexAction ia = new IndexAction();
            object session = Newtonsoft.Json.JsonConvert.DeserializeObject(System.IO.File.ReadAllText(Path.Combine("data", fileName)));
            JArray docArray = (JArray)(session);
            foreach (var document in docArray)
            {
                Document doc = new Document();
                doc.Add("product_id", Convert.ToString(document["product_id"]));
                doc.Add("product_class_id",     Convert.ToInt32(document["product_class_id"]));
                doc.Add("brand_name",           Convert.ToString(document["brand_name"]));
                doc.Add("product_name",         Convert.ToString(document["product_name"]));
                doc.Add("sku",                  Convert.ToString(document["SKU"]));
                doc.Add("srp",                  Convert.ToInt32(document["srp"]));
                doc.Add("gross_weight",         Convert.ToDouble(document["gross_weight"]));
                doc.Add("net_weight",           Convert.ToDouble(document["net_weight"]));
                doc.Add("recyclable_package",   Convert.ToString(document["recyclable_package"]));
                doc.Add("low_fat",              Convert.ToString(document["low_fat"]));
                doc.Add("units_per_case",       Convert.ToInt32(document["units_per_case"]));
                doc.Add("cases_per_pallet",     Convert.ToInt32(document["cases_per_pallet"]));
                doc.Add("shelf_width",          Convert.ToDouble(document["shelf_width"]));
                doc.Add("shelf_height",         Convert.ToDouble(document["shelf_height"]));
                doc.Add("shelf_depth",          Convert.ToDouble(document["shelf_depth"]));
                doc.Add("product_subcategory",  Convert.ToString(document["product_subcategory"]));
                doc.Add("product_category",     Convert.ToString(document["product_category"]));
                doc.Add("product_department",   Convert.ToString(document["product_department"]));
                doc.Add("product_family",       Convert.ToString(document["product_family"]));
                doc.Add("url", Convert.ToString(document["url"]));

                indexOperations.Add(new IndexAction(IndexActionType.Upload, doc));
            }

            IndexBatch(indexOperations);
        }

        private static void IndexBatch(List<IndexAction> changes)
        {
            try
            {
                _indexClient.Documents.Index(new IndexBatch(changes));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error uploading batch: {0}:\r\n", ex.Message.ToString());
            }
        }

        private static void CreateIndexer()
        {
            // This will use the Azure Search Indexer to synchronize data from Azure SQL to Azure Search
            // Since the field mappings is a preview feature it is not available yet in the Azure Search .NET API so I need to make a REST call
            Uri _serviceUri = new Uri("https://" + searchServiceName + ".search.windows.net");
            HttpClient _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

            Console.WriteLine("{0}", "Creating Data Source...\n");
            Uri uri = new Uri(_serviceUri, "datasources/foodmart-rec-ds");
            string json = "{ 'name' : 'foodmart-rec-ds','description' : 'FoodMart Recommendations DS','type' : 'azuresql','credentials' : { 'connectionString' : 'Server=tcp:" + azureSQLServer + ".database.windows.net,1433;Database=" + azureSQLDatabase + ";User ID=" + azureSQLUser + ";Password=" + azureSQLPassword + ";Trusted_Connection=False;Encrypt=True;Connection Timeout=30;' },'container' : { 'name' : 'ItemRecommendationsView' }} ";
            HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Put, uri, json);
            if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.NoContent)
            {
                Console.WriteLine("Error creating data source: {0}", response.Content.ReadAsStringAsync().Result);
                return;
            }

            Console.WriteLine("{0}", "Creating Indexer...\n");
            uri = new Uri(_serviceUri, "indexers/foodmart-rec-indexer");
            json = "{ 'name' : 'foodmart-rec-indexer','description' : 'FoodMart Recommendations indexer','dataSourceName' : 'foodmart-rec-ds','targetIndexName' : 'foodmart', " +
                "'parameters' : { 'maxFailedItems' : 10, 'maxFailedItemsPerBatch' : 5, 'base64EncodeKeys': false }, " +
                "'fieldMappings' : [ { 'sourceFieldName' : 'recommendations', 'mappingFunction' : { 'name' : 'jsonArrayToStringCollection' } } ] }";

            response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Put, uri, json);
            if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.NoContent)
            {
                Console.WriteLine("Error creating indexer: {0}", response.Content.ReadAsStringAsync().Result);
                return;
            }

        }

        private static void CreateAzureSQLTables()
        {
            // Create the Sales Order History (sales_fact) table as well as the Recommendations table
            string AzureSQLConnection = "Server=tcp:" + azureSQLServer + ".database.windows.net;Database=" + azureSQLDatabase + ";User ID=" + azureSQLUser + ";Password=" + azureSQLPassword + ";Trusted_Connection=False;Encrypt=True;MultipleActiveResultSets=True;";
            using (SqlConnection con = new SqlConnection(AzureSQLConnection))
            {
                con.Open();
                try
                {
                    Console.WriteLine("Creating sales_fact SQL table...");
                    string query = "CREATE TABLE [dbo].[sales_fact]([product_id] [int] NOT NULL,[time_id] [int] NOT NULL,[customer_id] [int] NULL, " +
                    "[promotion_id] [int] NULL,[store_id] [int] NULL,[store_sales] [numeric](18, 0) NULL,[store_cost] [numeric](18, 0) NULL, " +
                    "[unit_sales] [int] NULL,PRIMARY KEY CLUSTERED ([product_id] ASC,[time_id] ASC))";
                    SqlCommand command = new SqlCommand(query, con);
                    command.ExecuteNonQuery();

                    Console.WriteLine("Creating recommendations SQL table...");
                    query = "CREATE TABLE [dbo].[recommendations]([productid] [int] NOT NULL,[recommendation] [int] NOT NULL,[lastmodified] [datetime] NULL DEFAULT (getdate()), " +
                    "PRIMARY KEY CLUSTERED ([productid] ASC,[recommendation] ASC))";
                    command = new SqlCommand(query, con);
                    command.ExecuteNonQuery();

                    Console.WriteLine("Creating ItemRecommendationsView SQL view (used by Azure Search indexer)...");
                    query = "CREATE view [dbo].[ItemRecommendationsView] as " +
                        "SELECT distinct cast(R1.productid as varchar(10)) as product_id, '['+ " +
                        "STUFF ( ( " + 
                            "SELECT ',\"' + cast(recommendation as varchar(10)) + '\"' " +
                            "FROM recommendations R2 "+
                            "WHERE R2.ProductId = R1.ProductId " +
                            "ORDER BY recommendation FOR XML PATH('')), 1, 1, '')+ ']' AS recommendations, R1.lastmodified  " +
                            "FROM recommendations R1 Where lastmodified = (select max(lastmodified) from recommendations) ";
                    command = new SqlCommand(query, con);
                    command.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message.ToString());
                }
            }

        }

        private static void BCPDataIntoAzureSQLTable()
        {
            // Bulk upload data into the Sales Order History table (sales_fact) to be used by Mahout to create recommendations
            Console.WriteLine("Bulk Uploading Sales Order History data to Azure SQL...");
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "BCP.EXE";
            startInfo.Arguments = "sales_fact in data\\sales_fact.txt -S " + azureSQLServer + ".database.windows.net -d " + azureSQLDatabase + " -U " + azureSQLUser + " -P " + azureSQLPassword + " -n";
            Process.Start(startInfo);
        }

        private static void DownloadAndUnzipMahoutDistribution()
        {
            try
            {
                using (var client = new WebClient())
                {
                    Console.WriteLine("Downloading Mahout distribution (this can take some time)...");
                    client.DownloadFile("http://archive.apache.org/dist/mahout/0.10.0/mahout-distribution-0.10.0.zip", zipFile);
                }

                // Hard coding to c:\dist since there are a lot of sub-directories that can potentially go past max limit size
                Console.WriteLine("Uncompressing Mahout distribution to {0}...", extractPath);
                System.IO.Compression.ZipFile.ExtractToDirectory(zipFile, extractPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message.ToString());
            }
        }

        private static void UploadMahoutDistributiontoBlobStorage()
        {
            // Upload the Mahout distribution to Azure Blob Storage

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=http;AccountName=" + azureBlobStorageName + ";AccountKey=" + azureBlobStorageKey);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(azureBlobStorageContainer);
            CloudBlockBlob blockBlob;

            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            DirectoryInfo d = new DirectoryInfo(extractPath);

            foreach (var file in d.GetFiles("*.*", SearchOption.AllDirectories))
            {
                using (var fileStream = System.IO.File.OpenRead(file.FullName))
                {
                    string blockBlobRef = file.FullName.Substring(extractPath.Length + 1);
                    Console.WriteLine("Uploading {0}...", blockBlobRef);
                    blockBlob = container.GetBlockBlobReference(azureBlobDir + "\\" + blockBlobRef);
                    blockBlob.UploadFromStream(fileStream);
                }
            }

        }

        private static void UploadHiveQueryToBlobStorage()
        {
            // Upload the Hive Query file to Azure Blob Storage

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=http;AccountName=" + azureBlobStorageName + ";AccountKey=" + azureBlobStorageKey);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(azureBlobStorageContainer);
            CloudBlockBlob blockBlob;
            string HiveFile = "data\\selectsimilarproducts.hql";

            using (var fileStream = System.IO.File.OpenRead(HiveFile))
            {
                Console.WriteLine("Uploading Hive Query File {0}...", HiveFile);
                blockBlob = container.GetBlockBlobReference(HiveFile);
                blockBlob.UploadFromStream(fileStream);
            }

        }

    }
}

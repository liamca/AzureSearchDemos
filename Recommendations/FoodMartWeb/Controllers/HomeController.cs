using BingGeocoder;
using FoodMartWeb.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FoodMartWeb.Controllers
{
    public class HomeController : Controller
    {
        private Search _Search = new Search();
        
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Details()
        {
            return View();
        }

        public ActionResult Search(string q = "", string departmentFacet = "", string categoryFacet = "", string priceFacet = "", string lowFatFacet = "", 
            string sortType = "", int currentPage = 0)
        {
            // If blank search, assume they want to search everything
            if (string.IsNullOrWhiteSpace(q))
                q = "*";

            var response = _Search.SearchIndex(q, departmentFacet, categoryFacet, priceFacet, lowFatFacet, sortType, currentPage);
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = new GroceryItem() { Results = response.Results, Facets = response.Facets, Count = Convert.ToInt32(response.Count) }
            };
        }

        
        public ActionResult Recommendations(string relatedItems = "")
        {
            JArray ItemsArray = JArray.Parse(relatedItems);

            var response = _Search.GetProductIDs(ItemsArray);
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = new GroceryItem() { Results = response.Results }
            };
        }

        [HttpGet]
        public ActionResult Suggest(string term, bool fuzzy = true)
        {
            // Call suggest query and return results
            var response = _Search.Suggest(term, fuzzy);
            List<string> suggestions = new List<string>();
            foreach (var result in response)
            {
                suggestions.Add(result.Text);
            }

            // Remove duplicates
            List<string> noDupesSuggestions = suggestions.Distinct().ToList();

            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = noDupesSuggestions
            };

        }

        public ActionResult LookUp(string id)
        {
            // Take a key ID and do a lookup to get the job details
            if (id != null)
            {
                var response = _Search.LookUp(id);
                var suggestions = response.Document;
                return new JsonResult
                {
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    Data = new GroceryItemLookup() { Result = response.Document }
                };
            }
            else
            {
                return null;
            }

        }

    }
}
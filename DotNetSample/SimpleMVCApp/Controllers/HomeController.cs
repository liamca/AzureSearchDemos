using Microsoft.Azure.Search.Models;
using SimpleSearchMVCApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SimpleSearchMVCApp.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        private FeaturesSearch _featuresSearch = new FeaturesSearch();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Search(string q = "", string fiterField = "", string filter = "")
        {
            // If blank search, assume they want to search everything
            if (string.IsNullOrWhiteSpace(q))
                q = "*";

            var response = _featuresSearch.Search(q, fiterField, filter);
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = new SearchResponse() { Results = response.Results, Facets = response.Facets, Count = Convert.ToInt32(response.Count) }
            };
        }


    }
}

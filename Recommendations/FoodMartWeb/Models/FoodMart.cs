using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FoodMartWeb.Models
{
    public class GroceryItem
    {
        public FacetResults Facets { get; set; }
        public IList<SearchResult> Results { get; set; }
        public int? Count { get; set; }
    }

    public class GroceryItemLookup
    {
        public Document Result { get; set; }
    }

}
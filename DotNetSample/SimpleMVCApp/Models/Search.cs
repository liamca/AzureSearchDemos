using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
﻿using Microsoft.Azure.Search.Models;
using Microsoft.Spatial;

namespace SimpleSearchMVCApp.Models
{
    public class SearchResponse
    {
        public FacetResults Facets { get; set; }
        public IList<SearchResult> Results { get; set; }
        public int? Count { get; set; }
    }
}

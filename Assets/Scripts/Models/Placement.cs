using System;
using System.Collections;
using FeedFM.Utilities;

namespace FeedFM.Models
{
    internal class Placement
    {
        public int id { get; set; }
        public string Name { get; set; }
        public ArrayList Stations { get; set; }
        public JSONClass Options { get; set; }
    }
}
using System;
using System.Collections.Generic;
using FeedFM.Utilities;

namespace FeedFM.Models
{
    internal class Placement
    {
        public int id { get; set; }
        public string Name { get; set; }
        public List<Station> Stations { get; set; }
        public JSONClass Options { get; set; }
        
        public static Placement Parse(JSONClass jsonPlacement)
        {
            return new Placement
            {
                id = jsonPlacement["id"].AsInt,
                Name = jsonPlacement["name"].Value,
                Options = jsonPlacement["options"].AsObject
            };
        }
    }
}
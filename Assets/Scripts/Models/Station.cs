using System;
using System.Collections;
using System.Collections.Generic;
using FeedFM.Utilities;

namespace FeedFM.Models
{
    public class Station
    { 
        public int Id { get; set; }
        public string Name { get; set; }
        public JSONClass Options { get; set; }
        public List<AudioFile> AudioFiles { get; set; }
        public float PreGain { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsSinglePlay { get; set; }
        public bool IsOnDemand { get; set; }
        public string CastUrl { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime LastPlayStart { get; set; }
        public bool CanLike { get; set; }
        public bool CanSkip { get; set; }
        public bool IsTypeOffline { get; set; }

        public static Station Parse(JSONClass jStation)
        {
            Station station = new Station();
            station.Id = jStation["id"].AsInt;
            station.Name = jStation["name"].Value;
            station.CanLike = jStation["can_like"].AsBool;
            station.CanSkip = jStation["can_skip"].AsBool;
            station.Options = jStation["options"].AsObject;
            station.PreGain = jStation["pre_gain"].AsFloat;
            station.IsSinglePlay = jStation["single_play"].AsBool;
            station.IsOnDemand = jStation["on_demand"].AsBool;
            station.IsTypeOffline = false;

            if (jStation["last_updated"].Value.Length != 0) {
                station.LastUpdated = DateTime.Parse(jStation["last_updated"].Value);
            }
            if (jStation["expire_date"].Value.Length != 0)
            {
                station.ExpiryDate = DateTime.Parse(jStation["expire_date"].Value);
            }

            if (jStation["last_play_start"].Value.Length != 0)
            {
                station.LastPlayStart = DateTime.Parse(jStation["last_play_start"].Value);
            }

            return station;
        }
    }
}
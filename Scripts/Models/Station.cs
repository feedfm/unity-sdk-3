using System;
using System.Collections;
using FeedFM.Utilities;

namespace FeedFM.Models
{
    internal class Station
    { 
        public int Id { get; set; }
        public string Name { get; set; }
        public JSONClass Options { get; set; }
        public ArrayList AudioFiles { get; set; }
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
    }
}
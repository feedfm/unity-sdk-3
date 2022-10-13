
using FeedFM.Utilities;

namespace FeedFM.Models
{
    internal class AudioFile
    {
        public string TrackTitle { get; set; }
        public string ReleaseTitle { get; set; }
        public string ArtistTitle { get; set; }
        public int Id { get; set; }
        public float DurationInSeconds { get; set; }
        public string Codec { get; set; }
        public string Bitrate { get; set; }
        public float ReplayGain { get; set; }
        public string Url { get; set; }
    
        public JSONClass MetaData {get; set; }
        public bool IsLiked { get; set; }
        public bool IsDisliked { get; set; }

        public float trimStart
        {
            get
            {
                float trim = 0.0f;
                trim =  MetaData["trim_start"].AsFloat;
                return trim;
            }
        }

        public float trimEnd {
            get
            {
                float trim = 0.0f;
                trim =  MetaData["trim_end"].AsFloat;
                return trim;
            }
        }

    }
}
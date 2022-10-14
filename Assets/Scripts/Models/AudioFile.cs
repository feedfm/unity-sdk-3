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

        public float trimStart => MetaData["trim_start"].AsFloat;

        public float trimEnd => MetaData["trim_end"].AsFloat;

        public static AudioFile Parse(JSONClass jfile)
        {
            return new AudioFile()
            {
                Id = jfile["id"].AsInt,
                ArtistTitle = jfile["artist"].AsObject["name"].Value,
                ReleaseTitle = jfile["release"].AsObject["title"].Value,
                TrackTitle = jfile["track"].AsObject["title"].Value,
                Bitrate = jfile["bitrate"].Value,
                DurationInSeconds = jfile["duration_in_seconds"].AsFloat,
                IsDisliked = jfile["disliked"].AsBool,
                Codec = jfile["codec"].Value,
                Url = jfile["url"].Value,
                IsLiked = jfile["liked"].AsBool,
                MetaData = jfile["extra"].AsObject,
                ReplayGain = jfile["replay_gain"].AsFloat
            };
        }
    }
}
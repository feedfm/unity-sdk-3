using FeedFM.Utilities;

namespace FeedFM.Models
{
    public class Play
    {
        public string Id { get; set; }
        public Station Station { get; set; }
        public AudioFile AudioFile { get; set; }

        public static Play Parse(JSONNode play)
        {
            return new Play()
            {
                Id = play["id"].Value,
                AudioFile = AudioFile.Parse(play["audio_file"].AsObject),
                Station = Station.Parse(play["station"].AsObject)

            };
        }
    }
}
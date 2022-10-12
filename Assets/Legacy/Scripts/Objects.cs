using System;
using System.Collections;
using SimpleJSON;


public enum PlayerState {
    Uninitialized,
    Unavailable,
    ReadyToPlay,
    Stalled,
    Playing,
    Paused,
    WaitingForItem,
    Exhausted
}

public class AudioFile
{
    public String TrackTitle { get; set; }
    public String ReleaseTitle { get; set; }
    public String ArtistTitle { get; set; }
    public int Id { get; set; }
    public float DurationInSeconds { get; set; }
    public String Codec { get; set; }
    public String Bitrate { get; set; }
    public float ReplayGain { get; set; }
    public String Url { get; set; }
    
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

public class Play
{
    public string Id { get; set; }
    public Station Station { get; set; }
    public AudioFile AudioFile { get; set; }
}



public class Station { 
    public int Id { get; set; }
    public String Name { get; set; }
    public JSONClass Options { get; set; }

    public ArrayList AudioFiles { get; set; }
    public float PreGain { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsSinglePlay { get; set; }
    public bool IsOnDemand { get; set; }
    public String CastUrl { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime LastPlayStart { get; set; }
    public bool CanLike { get; set; }
    public bool CanSkip { get; set; }
    public bool IsTypeOffline { get; set; }
}



public class Placement
{
    public int id { get; set; }
    public String Name { get; set; }
    public ArrayList Stations { get; set; }
    public JSONClass Options { get; set; }
}


public delegate void StationDelegate(Station station);
public delegate void PlayHandler(Play play);
public delegate void ProgressHandler(Play play, float progress, float duration);
public delegate void StateHandler(PlayerState state);
public delegate void SessionDelegate(bool isAvailable, String errMsg);
public delegate void ClientDelegate(String clientID);



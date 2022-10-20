using System;
using FeedFM.Models;

namespace FeedFM.Utilities
{
    #region FeedPlayer
    
    public delegate void StationDelegate(Station station);

    #endregion
    
    #region MixingAudioPlayer
        
    public delegate void PlayHandler(Play play);
    public delegate void ProgressHandler(Play play, float progress, float duration);
    public delegate void StateHandler(PlayerState state);

    #endregion
    
    #region Session
    public delegate void SessionDelegate(bool isAvailable, string errMsg);
    public delegate void ClientDelegate(string clientID);
    internal delegate void SkipDelegate(bool isSuccess);
    internal delegate void PlayDelegate(Play data);
    internal delegate void Handler(Session session);

    #endregion
}
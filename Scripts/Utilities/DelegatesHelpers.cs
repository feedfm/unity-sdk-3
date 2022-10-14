using System;
using FeedFM.Models;

namespace FeedFM.Utilities
{
    internal delegate void StationDelegate(Station station);
    internal delegate void PlayHandler(Play play);
    internal delegate void ProgressHandler(Play play, float progress, float duration);
    internal delegate void StateHandler(PlayerState state);
    internal delegate void SessionDelegate(bool isAvailable, string errMsg);
    internal delegate void ClientDelegate(string clientID);
    internal delegate void SkipDelegate(bool isSuccess);
    internal delegate void PlayDelegate(Play data);
    internal delegate void Handler(Session obj);
}
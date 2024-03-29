﻿namespace FeedFM.Models
{
    internal enum FeedError : int
    {
        None = 0,
        UndefinedError = -1,
        BadCredentials = 5,
        Forbidden = 6,
        SkipDenied = 7,
        NoMoreMusic = 9,
        PlayNotActive = 12,
        InvalidParameters = 15,
        MissingParameter = 16,
        MissingObject = 17,
        InternalError = 18,
        InvalidRegion = 19,
        PlaybackStarted = 20,
        PlaybackComplete = 21,
        ConnectionError = 500
    }
}
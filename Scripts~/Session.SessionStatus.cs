using System;
using FeedFM.Models;

namespace FeedFM
{
    internal class SessionStatus {
        public Play play;  // POST /play response from server
        public Boolean started; // true if we started playback
        public Boolean canSkip; // true if we can skip this song
        public int retryCount;  // number of times we've unsuccessfully asked server if we can start play
    }
}
namespace FeedFM.Models
{
    internal enum PlayerState
    {
        Uninitialized,
        Unavailable,
        ReadyToPlay,
        Stalled,
        Playing,
        Paused,
        WaitingForItem,
        Exhausted
    }
}
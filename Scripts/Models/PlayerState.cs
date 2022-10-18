namespace FeedFM.Models
{
    public enum PlayerState
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
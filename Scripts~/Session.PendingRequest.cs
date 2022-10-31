using FeedFM.Utilities;

namespace FeedFM
{
    internal class PendingRequest {
        public Ajax ajax;  // outstanding POST /play request
        public int retryCount; // number of times we've retried this request
    }
}
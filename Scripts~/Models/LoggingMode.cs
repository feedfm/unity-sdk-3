namespace FeedFM.Models
{
    enum LoggingMode
    {
        /// <summary>
        /// Never log
        /// </summary>
        None,
        
        /// <summary>
        /// Log only while in editor
        /// </summary>
        EditorOnly,
        
        /// <summary>
        /// Always log
        /// </summary>
        Always
    }
}
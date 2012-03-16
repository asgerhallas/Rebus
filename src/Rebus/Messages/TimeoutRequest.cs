using System;

namespace Rebus.Messages
{
    /// <summary>
    /// Requests a delayed reply from the Timeout Service. Upon receiving
    /// this message, the Timeout Service will calculate the UTC time of when the timeout
    /// should expire, wait, and then reply with a <see cref="TimeoutReply"/>.,
    /// </summary>
    public class TimeoutRequest : IRebusControlMessage
    {
        /// <summary>
        /// For how long should the reply be delayed?
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Allows for specifying a correlation ID that the Timeout Service will
        /// return with the <see cref="TimeoutReply"/>.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Allows for specifying payload data that the Timeout Service will
        /// return with the <see cref="TimeoutReply"/>.
        /// </summary>
        public string Data { get; set; }
    }
}
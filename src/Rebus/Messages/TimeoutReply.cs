using System;

namespace Rebus.Messages
{
    /// <summary>
    /// This is the reply that the Timeout Service will send back to the
    /// timeout requestor upon completion of the timeout.
    /// </summary>
    public class TimeoutReply : IRebusControlMessage
    {
        /// <summary>
        /// The UTC time of when the timeout expired.
        /// </summary>
        public DateTime DueTime { get; set; }

        /// <summary>
        /// The correlation ID as specified in the <see cref="TimeoutRequest"/>.
        /// </summary>
        public string CorrelationId { get; set; }

    }

    /// <summary>
    /// This is the reply that the Timeout Service will send back to the
    /// timeout requestor upon completion of the timeout.
    /// The reply will include the payload data sent in the original request.
    /// </summary>
    public class TimeoutReply<TData> : TimeoutReply
    {
        /// <summary>
        /// Payload data as given in the <see cref="TimeoutRequest"/>
        /// </summary>
        public TData Data { get; set; }
    }
}
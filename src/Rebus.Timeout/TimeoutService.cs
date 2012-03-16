using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Transactions;
using Rebus.Bus;
using Rebus.Log4Net;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Serialization.Json;
using Rebus.Transports.Msmq;
using log4net;
using ILog = log4net.ILog;

namespace Rebus.Timeout
{
    public class TimeoutService : IHandleMessages<TimeoutRequest>, IActivateHandlers
    {
        const string InputQueueName = "rebus.timeout";
        static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        static readonly Type[] IgnoredMessageTypes = new[] {typeof (object), typeof (IRebusControlMessage)};

        readonly IBus bus;
        readonly RebusBus rebusBus;
        readonly IStoreTimeouts storeTimeouts;
        readonly Timer timer = new Timer();

        public TimeoutService(IStoreTimeouts storeTimeouts)
        {
            this.storeTimeouts = storeTimeouts;
            var msmqMessageQueue = new MsmqMessageQueue(InputQueueName);

            RebusLoggerFactory.Current = new Log4NetLoggerFactory();
            rebusBus = new RebusBus(this, msmqMessageQueue, msmqMessageQueue, null, null, null,
                                    new JsonMessageSerializer(), new TrivialPipelineInspector());
            bus = rebusBus;

            timer.Interval = 300;
            timer.Elapsed += CheckCallbacks;
        }

        public string InputQueue
        {
            get { return InputQueueName; }
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            if (typeof (TimeoutRequest).IsAssignableFrom(typeof (T)))
            {
                return new[] {(IHandleMessages<T>) this};
            }

            if (IgnoredMessageTypes.Contains(typeof (T)))
            {
                return new IHandleMessages<T>[0];
            }

            throw new InvalidOperationException(string.Format("Someone took the chance and sent a message of " +
                                                              "type {0} to me, though I only handle TimeoutRequests.", typeof (T)));
        }

        public void Release(IEnumerable handlerInstances)
        {
        }

        public void Handle(TimeoutRequest message)
        {
            var currentMessageContext = MessageContext.GetCurrent();

            var data = GetDataFromTimeoutRequest(message);

            var newTimeout = new Timeout
                             {
                                 CorrelationId = message.CorrelationId,
                                 ReplyTo = currentMessageContext.ReturnAddress,
                                 TimeToReturn = DateTime.UtcNow + message.Timeout,
                                 Data = data,
                                 DataType = data != null ? data.GetType() : null
                             };

            storeTimeouts.Add(newTimeout);

            Log.InfoFormat("Added new timeout: {0}", newTimeout);
        }

        static object GetDataFromTimeoutRequest(TimeoutRequest message)
        {
            var dataProperty = message.GetType().GetProperty("Data");
            var data = dataProperty != null ? dataProperty.GetValue(message, null) : null;
            return data;
        }

        public void Start()
        {
            Log.Info("Starting bus");
            rebusBus.Start(1);
            Log.Info("Starting inner timer");
            timer.Start();
        }

        public void Stop()
        {
            Log.Info("Stopping inner timer");
            timer.Stop();
            Log.Info("Disposing bus");
            rebusBus.Dispose();
        }

        void CheckCallbacks(object sender, ElapsedEventArgs e)
        {
            using (var tx = new TransactionScope())
            {
                var dueTimeouts = storeTimeouts.RemoveDueTimeouts();

                foreach (var timeout in dueTimeouts)
                {
                    Log.InfoFormat("Timeout!: {0} -> {1}", timeout.CorrelationId, timeout.ReplyTo);

                    var reply = CreateTimeoutReply(timeout);

                    reply.CorrelationId = timeout.CorrelationId;
                    reply.DueTime = timeout.TimeToReturn;

                    bus.Send(timeout.ReplyTo, reply);
                }

                tx.Complete();
            }
        }

        static TimeoutReply CreateTimeoutReply(Timeout timeout)
        {
            if (timeout.Data == null)
                return new TimeoutReply();

            var reply = (TimeoutReply) Activator.CreateInstance(typeof (TimeoutReply<>).MakeGenericType(timeout.DataType));
            var dataProperty = reply.GetType().GetProperty("Data");
            dataProperty.SetValue(reply, timeout.Data, null);
            return reply;
        }
    }
}
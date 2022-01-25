// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventLogs
{
    /// <summary>
    /// Represents an implementation of <see cref="IQueueCacheCursor"/> for MongoDB event log.
    /// </summary>
    public class EventLogQueueCacheCursor : IQueueCacheCursor
    {
        readonly IExecutionContextManager _executionContextManager;
        readonly IEventLogStorageProvider _eventLogStorageProvider;
        readonly IStreamIdentity _streamIdentity;
        readonly IEnumerable<EventType> _eventTypes;
        readonly EventSourceId? _partition;
        IEventCursor? _cursor;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogQueueCacheCursor"/>.
        /// </summary>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
        /// <param name="eventLogStorageProvider"><see cref="IEventLogStorageProvider"/> for getting events from storage.</param>
        /// <param name="streamIdentity"><see cref="IStreamIdentity"/> for the stream.</param>
        /// <param name="token"><see cref="StreamSequenceToken"/> that represents the starting point to get from.</param>
        /// <param name="eventTypes">Optional collection of <see cref="EventType">Event types</see> to filter the cursor with - default all.</param>
        /// <param name="partition">Optional <see cref="EventSourceId"/> partition to filter for.</param>
        public EventLogQueueCacheCursor(
            IExecutionContextManager executionContextManager,
            IEventLogStorageProvider eventLogStorageProvider,
            IStreamIdentity streamIdentity,
            StreamSequenceToken token,
            IEnumerable<EventType>? eventTypes = default,
            EventSourceId? partition = default)
        {
            _executionContextManager = executionContextManager;
            _eventLogStorageProvider = eventLogStorageProvider;
            _streamIdentity = streamIdentity;
            _eventTypes = eventTypes ?? Array.Empty<EventType>();
            _partition = partition;
            FindEventsFrom(token);
        }

        /// <inheritdoc/>
        public IBatchContainer GetCurrent(out Exception exception)
        {
            exception = null!;
            if (_cursor == null) return null!;

            try
            {
                var appendedEvents = _cursor.Current.ToArray();
                if (appendedEvents.Length == 0)
                {
                    return null!;
                }

                return new EventLogBatchContainer(
                    appendedEvents,
                    _streamIdentity.Guid,
                    _streamIdentity.Namespace,
                    new Dictionary<string, object> { { RequestContextKeys.TenantId, _streamIdentity.Namespace } });
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            return null!;
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_cursor is null) return false;

            var task = _cursor.MoveNext();
            task.Wait();
            return task.Result;
        }

        /// <inheritdoc/>
        public void RecordDeliveryFailure()
        {
        }

        /// <inheritdoc/>
        public void Refresh(StreamSequenceToken token)
        {
            FindEventsFrom(token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _cursor?.Dispose();
            _cursor = null!;
        }

        void FindEventsFrom(StreamSequenceToken token)
        {
            TenantId tenantId = _streamIdentity.Namespace;
            _executionContextManager.Establish(tenantId, CorrelationId.New());
            var task = _eventLogStorageProvider.GetFromSequenceNumber((ulong)token.SequenceNumber, _partition, _eventTypes);
            task.Wait();
            _cursor = task.Result;
        }
    }
}

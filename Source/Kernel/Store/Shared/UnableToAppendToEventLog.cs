// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Events.Store
{
    /// <summary>
    /// Exception that gets thrown when the storage mechanism is not able to append the event to the event log.
    /// </summary>
    public class UnableToAppendToEventLog : Exception
    {
        /// <summary>
        /// Gets the stream identifier.
        /// </summary>
        public Guid StreamId { get; }

        /// <summary>
        /// Gets the tenant identifier.
        /// </summary>
        public TenantId TenantId { get; }

        /// <summary>
        /// Gets the sequence number within the event log.
        /// </summary>
        public EventLogSequenceNumber SequenceNumber { get; }

        /// <summary>
        /// Gets the event source identifier.
        /// </summary>
        public EventSourceId EventSourceId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnableToAppendToEventLog"/> class.
        /// </summary>
        /// <param name="streamId">The stream that is failing.</param>
        /// <param name="tenantId">For which tenant it is.</param>
        /// <param name="sequenceNumber">The sequence number that is failing.</param>
        /// <param name="eventSourceId">EventSource it is failing for.</param>
        /// <param name="innerException">The inner exception.</param>
        public UnableToAppendToEventLog(Guid streamId, TenantId tenantId, EventLogSequenceNumber sequenceNumber, EventSourceId eventSourceId, Exception innerException)
            : base($"Unable to append event at sequence {sequenceNumber} for event source {eventSourceId} on tenant {tenantId} from stream {streamId}", innerException)
        {
            StreamId = streamId;
            TenantId = tenantId;
            SequenceNumber = sequenceNumber;
            EventSourceId = eventSourceId;
        }
    }
}

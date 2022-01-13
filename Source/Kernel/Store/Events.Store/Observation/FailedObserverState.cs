// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Cratis.Events.Store.Observation
{
    /// <summary>
    /// Represents the state used for failed observers.
    /// </summary>
    [Serializable]
    public class FailedObserverState
    {
        string _id;

        /// <summary>
        /// The name of the storage provider used for working with this type of state.
        /// </summary>
        public const string StorageProvider = "failed-observer-state-provider";

        /// <summary>
        /// Gets the unique identifier for the state.
        /// </summary>
        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                var (eventLogId, observerId, eventSourceId) = Parse(value);
                EventLogId = eventLogId;
                ObserverId = observerId;
                EventSourceId = eventSourceId;
            }
        }

        /// <summary>
        /// Gets the event log identifier.
        /// </summary>
        public EventLogId EventLogId { get; private set; }

        /// <summary>
        /// Gets the observer identifier.
        /// </summary>
        public ObserverId ObserverId { get; private set; }

        /// <summary>
        /// Gets the event source identifier.
        /// </summary>
        public EventSourceId EventSourceId { get; private set; }

        /// <summary>
        /// Gets or sets whether or not the partition has failed.
        /// </summary>
        public bool IsFailed { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventLogSequenceNumber"/> of the failure - if any.
        /// </summary>
        public EventLogSequenceNumber SequenceNumber { get; set; } = EventLogSequenceNumber.First;

        /// <summary>
        /// Gets or sets the event types for the observer.
        /// </summary>
        public IEnumerable<EventType> EventTypes { get; set; } = Array.Empty<EventType>();

        /// <summary>
        /// Gets or sets the occurred time of the failure - if any.
        /// </summary>
        public DateTimeOffset Occurred { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the number of retry attempts it has had.
        /// </summary>
        public int Attempts { get; set; }

        /// <summary>
        /// Gets or sets the message from the failure - if any.
        /// </summary>
        public string[] Messages { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the stack trace from the failure - if any.
        /// </summary>
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// Create a composite key.
        /// </summary>
        /// <param name="eventLogId">The Event log.</param>
        /// <param name="observerId">The observer.</param>
        /// <param name="eventSourceId">The event source.</param>
        /// <returns>The composite key.</returns>
        public static string CreateKeyFrom(EventLogId eventLogId, ObserverId observerId, EventSourceId eventSourceId) => $"{eventLogId}+{observerId}+{eventSourceId}";

        /// <summary>
        /// Parse a string representation of a composite key.
        /// </summary>
        /// <param name="key">Key to parse.</param>
        /// <returns>Tuple holding event log, observer and event source.</returns>
        public static (EventLogId eventLogId, ObserverId observerId, EventSourceId eventSourceId) Parse(string key)
        {
            var segments = key.Split('+');
            return (
                segments[0],
                segments[1],
                segments[2]
            );
        }
    }
}

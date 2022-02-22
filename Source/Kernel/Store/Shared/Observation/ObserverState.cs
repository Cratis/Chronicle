// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Aksio.Cratis.Events.Store.Observation
{
    /// <summary>
    /// Represents the state used for an observer.
    /// </summary>
    public class ObserverState
    {
        /// <summary>
        /// The name of the storage provider used for working with this type of state.
        /// </summary>
        public const string StorageProvider = "observer-state";

        /// <summary>
        /// Gets or sets the identifier of the observer state.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the event types the observer is observing.
        /// </summary>
        public IEnumerable<EventType> EventTypes { get; set; } = Array.Empty<EventType>();

        /// <summary>
        /// Gets or sets the <see cref="EventLogId"/> the state is for.
        /// </summary>
        public EventSequenceId EventLogId { get; set; } = EventSequenceId.Unspecified;

        /// <summary>
        /// Gets or sets the <see cref="EventLogId"/> the state is for.
        /// </summary>
        public ObserverId ObserverId { get; set; } = ObserverId.Unspecified;

        /// <summary>
        /// Gets or sets the current offset into the event log.
        /// </summary>
        public EventSequenceNumber Offset { get; set; } = EventSequenceNumber.First;

        /// <summary>
        /// Gets or sets the last handled event in the event log, ever. This value will never reset during a rewind.
        /// </summary>
        public EventSequenceNumber LastHandled { get; set; } = EventSequenceNumber.First;
    }
}

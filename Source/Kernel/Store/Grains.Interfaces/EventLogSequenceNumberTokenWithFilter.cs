// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains
{
    /// <summary>
    /// Represents a <see cref="StreamSequenceToken"/> for observers.
    /// </summary>
    public class EventLogSequenceNumberTokenWithFilter : EventLogSequenceNumberToken
    {
        /// <summary>
        /// Gets the collection of <see cref="EventType">event types</see> the observer is interested in.
        /// </summary>
        public IEnumerable<EventType> EventTypes { get; }

        /// <summary>
        /// Gets the <see cref="EventSourceId"/> partition.
        /// </summary>
        public EventSourceId Partition { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogSequenceNumberTokenWithFilter"/> class.
        /// </summary>
        /// <param name="sequenceNumber"><see cref="EventLogSequenceNumber"/>.</param>
        /// <param name="eventTypes"><see cref="EventType">event types</see> the observer is interested in.</param>
        /// <param name="partition">Optional <see cref="EventSourceId"/> partition.</param>
        public EventLogSequenceNumberTokenWithFilter(EventLogSequenceNumber sequenceNumber, IEnumerable<EventType> eventTypes, EventSourceId? partition = default) : base(sequenceNumber)
        {
            EventTypes = eventTypes;
            Partition = partition ?? EventSourceId.Unspecified;
        }

        /// <inheritdoc/>
        public override bool Equals(StreamSequenceToken other)
        {
            if (other is EventLogSequenceNumberTokenWithFilter observerToken)
            {
                return
                    observerToken.EventTypes.SequenceEqual(EventTypes) &&
                    observerToken.Partition.Equals(Partition) &&
                    base.Equals(other);
            }

            return base.Equals(other);
        }

        /// <inheritdoc/>
        public override int CompareTo(StreamSequenceToken other) => other.SequenceNumber.CompareTo(SequenceNumber);
    }
}

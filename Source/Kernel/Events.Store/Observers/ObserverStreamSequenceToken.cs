// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Cratis.Events.Store.Observers
{
    /// <summary>
    /// Represents a <see cref="StreamSequenceToken"/> for observers.
    /// </summary>
    [Serializable]
    public class ObserverStreamSequenceToken : EventSequenceToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObserverStreamSequenceToken"/> class.
        /// </summary>
        /// <param name="sequenceNumber"><see cref="EventLogSequenceNumber"/></param>
        /// <param name="eventTypes"><see cref="EventType">event types</see> the observer is interested in.</param>
        public ObserverStreamSequenceToken(EventLogSequenceNumber sequenceNumber, IEnumerable<EventType> eventTypes) : base((long)sequenceNumber.Value)
        {
            EventTypes = eventTypes;
        }

        /// <summary>
        /// Gets the collection of <see cref="EventType">event types</see> the observer is interested in.
        /// </summary>
        public IEnumerable<EventType> EventTypes { get; }

        /// <inheritdoc/>
        public override bool Equals(StreamSequenceToken other)
        {
            if (other is ObserverStreamSequenceToken observerToken)
            {
                return observerToken.EventTypes.SequenceEqual(EventTypes) && base.Equals(other);
            }

            return base.Equals(other);
        }

        /// <inheritdoc/>
        public override int CompareTo(StreamSequenceToken other) => other.SequenceNumber.CompareTo(SequenceNumber);
    }
}

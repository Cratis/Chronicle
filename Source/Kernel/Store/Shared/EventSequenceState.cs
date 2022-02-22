// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Aksio.Cratis.Events.Store
{
    /// <summary>
    /// Represents the state used by the event log. This state is meant to be per event log instance.
    /// </summary>
    public class EventSequenceState
    {
        /// <summary>
        /// The name of the storage provider used for working with this type of state.
        /// </summary>
        public const string StorageProvider = "event-sequence-state";

        /// <summary>
        /// Gets or sets the next sequencenumber (tail).
        /// </summary>
        public EventSequenceNumber SequenceNumber { get; set; } = EventSequenceNumber.First;
    }
}

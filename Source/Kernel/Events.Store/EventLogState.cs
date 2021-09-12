// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Cratis.Events.Store
{
    /// <summary>
    /// Represents the state used by the event log. This state is meant to be per event log instance.
    /// </summary>
    [Serializable]
    public class EventLogState
    {
        /// <summary>
        /// The name of the storage provider used for working with this type of state.
        /// </summary>
        public const string StorageProvider = "EventLogStorage";

        /// <summary>
        /// Gets or sets the <see cref="EventLogId"/> for the state.
        /// </summary>
        public EventLogId EventLog { get; set; }

        /// <summary>
        /// Gets or sets the next sequencenumber (tail).
        /// </summary>
        public uint SequenceNumber { get; set; }
    }
}

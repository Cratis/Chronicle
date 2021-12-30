// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Cratis.Events.Store.Observation
{
    /// <summary>
    /// Represents the state used by partitioned observers.
    /// </summary>
    [Serializable]
    public class PartitionedObserverState
    {
        /// <summary>
        /// The name of the storage provider used for working with this type of state.
        /// </summary>
        public const string StorageProvider = "partitioned-observer-state";

        /// <summary>
        /// Gets or sets whether or not the partition has failed.
        /// </summary>
        public bool IsFailed { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventLogSequenceNumber"/> of the failure - if any.
        /// </summary>
        public EventLogSequenceNumber SequenceNumber { get; set; }

        /// <summary>
        /// Gets or sets the occurred time of the failure - if any.
        /// </summary>
        public DateTimeOffset Occurred { get; set; }

        /// <summary>
        /// Gets or sets the message from the failure - if any.
        /// </summary>
        public string[] Messages { get; set; }

        /// <summary>
        /// Gets or sets the stack trace from the failure - if any.
        /// </summary>
        public string StackTrace { get; set; }
    }
}

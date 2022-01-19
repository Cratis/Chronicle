// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store
{
    /// <summary>
    /// Represents the unique identifier of an event log.
    /// </summary>
    /// <param name="Value">Actual value.</param>
    public record EventLogId(Guid Value) : ConceptAs<Guid>(Value)
    {
        /// <summary>
        /// The <see cref="EventLogId"/> representing an unspecified value.
        /// </summary>
        public static readonly EventLogId Unspecified = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        /// <summary>
        /// The <see cref="EventLogId"/> representing the default event log.
        /// </summary>
        public static readonly EventLogId Default = Guid.Empty;

        /// <summary>
        /// The <see cref="EventLogId"/> representing the default public event log.
        /// </summary>
        public static readonly EventLogId Public = Guid.Parse("ae99de1e-b19f-4a33-a5c4-3908508ce59f");

        /// <summary>
        /// Implicitly convert from a string representation of a <see cref="Guid"/> to <see cref="EventLogId"/>.
        /// </summary>
        /// <param name="id"><see cref="Guid"/> to convert from.</param>
        public static implicit operator EventLogId(string id) => new(Guid.Parse(id));

        /// <summary>
        /// Implicitly convert from <see cref="Guid"/> to <see cref="EventLogId"/>.
        /// </summary>
        /// <param name="id"><see cref="Guid"/> to convert from.</param>
        public static implicit operator EventLogId(Guid id) => new(id);

        /// <summary>
        /// Get whether or not this is the default event log.
        /// </summary>
        public bool IsDefault => this == Default;

        /// <summary>
        /// Get whether or not this is the public event log.
        /// </summary>
        public bool IsPublic => this == Public;
    }
}

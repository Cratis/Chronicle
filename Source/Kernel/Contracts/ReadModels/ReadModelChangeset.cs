// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents a changeset for a read model.
/// </summary>
[ProtoContract]
public class ReadModelChangeset
{
    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(1)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model key.
    /// </summary>
    [ProtoMember(2)]
    public string ModelKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model as JSON.
    /// </summary>
    [ProtoMember(3)]
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the read model was removed.
    /// </summary>
    [ProtoMember(4)]
    public bool Removed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this frame is the initial readiness signal emitted
    /// by the server once the underlying change stream subscription has been established. When
    /// <see langword="true"/>, the frame carries no actual read model payload and exists only to
    /// notify the client that subsequent changesets will be delivered.
    /// </summary>
    [ProtoMember(5)]
    public bool Subscribed { get; set; }

    /// <summary>
    /// Gets or sets the type of change that occurred to the read model instance.
    /// </summary>
    [ProtoMember(6)]
    public ReadModelChangeType ChangeType { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the event that caused the change.
    /// </summary>
    [ProtoMember(7)]
    public ulong EventSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the time at which the event that caused the change occurred.
    /// </summary>
    [ProtoMember(8)]
    public SerializableDateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the correlation id of the event that caused the change.
    /// </summary>
    [ProtoMember(9)]
    public Guid CorrelationId { get; set; } = Guid.Empty;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents a snapshot of a read model at a specific point in time.
/// </summary>
[ProtoContract]
public class ReadModelSnapshot
{
    /// <summary>
    /// Gets or sets the read model as JSON.
    /// </summary>
    [ProtoMember(1)]
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the events that were applied to create this snapshot.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<AppendedEvent> Events { get; set; } = [];

    /// <summary>
    /// Gets or sets when the snapshot occurred.
    /// </summary>
    [ProtoMember(3)]
    public SerializableDateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public Guid CorrelationId { get; set; } = Guid.Empty;
}

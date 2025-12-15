// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents a single projection snapshot.
/// </summary>
[ProtoContract]
public class ProjectionSnapshot
{
    /// <summary>
    /// Gets or sets the projected read model as JSON.
    /// </summary>
    [ProtoMember(1)]
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the events that were applied as JSON array.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<AppendedEvent> Events { get; set; } = [];

    /// <summary>
    /// Gets or sets when the first event occurred.
    /// </summary>
    [ProtoMember(3)]
    public SerializableDateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the CorrelationId.
    /// </summary>
    [ProtoMember(4)]
    public Guid CorrelationId { get; set; }
}

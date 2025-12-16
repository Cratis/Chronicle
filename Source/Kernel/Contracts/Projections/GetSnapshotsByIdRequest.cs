// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the request for getting snapshots by CorrelationId.
/// </summary>
[ProtoContract]
public class GetSnapshotsByIdRequest
{
    /// <summary>
    /// Gets or sets the projection identifier.
    /// </summary>
    [ProtoMember(1)]
    public string ProjectionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(2)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(3)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [ProtoMember(4)]
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model key.
    /// </summary>
    [ProtoMember(5)]
    public string ReadModelKey { get; set; }
}

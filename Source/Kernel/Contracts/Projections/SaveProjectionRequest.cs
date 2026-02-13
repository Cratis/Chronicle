// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the request for saving a projection from its projection declaration language representation.
/// </summary>
[ProtoContract]
public class SaveProjectionRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace name.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence id the projection targets.
    /// </summary>
    [ProtoMember(3), DefaultValue("event-log")]
    public string EventSequenceId { get; set; } = "event-log";

    /// <summary>
    /// Gets or sets the projection declaration language representation of the projection.
    /// </summary>
    [ProtoMember(4)]
    public string Declaration { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional draft read model definition.
    /// When provided, this will be saved as a new read model type before saving the projection.
    /// </summary>
    [ProtoMember(5)]
    public DraftReadModelDefinition? DraftReadModel { get; set; }
}

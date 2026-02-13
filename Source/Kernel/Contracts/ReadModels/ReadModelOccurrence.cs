// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents an occurrence of a replayed read model.
/// </summary>
[ProtoContract]
public class ReadModelOccurrence
{
    /// <summary>
    /// Gets or sets the <see cref="ObserverId"/> for the observer that owned the occurrence.
    /// </summary>
    [ProtoMember(1)]
    public string ObserverId { get; set; }

    /// <summary>
    /// Gets or sets when the occurrence happened.
    /// </summary>
    [ProtoMember(2)]
    public SerializableDateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ReadModelType"/> of the read model.
    /// </summary>
    [ProtoMember(3)]
    public ReadModelType Type { get; set; }

    /// <summary>
    /// Gets or sets the container name of the read model (collection, table, etc.).
    /// </summary>
    [ProtoMember(4)]
    public string ContainerName { get; set; }

    /// <summary>
    /// Gets or sets the container name of the revert read model (collection, table, etc.).
    /// </summary>
    [ProtoMember(5)]
    public string RevertContainerName { get; set; }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Cuts;

/// <summary>
/// Represents the published manifest resulting from a <see cref="ReadModelCutRequest"/>.
/// </summary>
[ProtoContract]
public class ReadModelCutResponse
{
    /// <summary>
    /// Gets or sets the deterministic identifier of the manifest.
    /// </summary>
    [ProtoMember(1)]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the exact position, per event sequence, every entry is bound to.
    /// </summary>
    [ProtoMember(2)]
    public IEnumerable<EventSequenceCut> Cuts { get; set; } = [];

    /// <summary>
    /// Gets or sets the outcome for every read model in the requested selection.
    /// </summary>
    [ProtoMember(3)]
    public IEnumerable<ReadModelCutEntry> Entries { get; set; } = [];

    /// <summary>
    /// Gets or sets when the manifest was published.
    /// </summary>
    [ProtoMember(4)]
    public DateTimeOffset PublishedAt { get; set; }
}

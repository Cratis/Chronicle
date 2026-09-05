// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Cuts;

/// <summary>
/// Represents the outcome and, on success, the payload digest for one read model in a <see cref="ReadModelCutResponse"/>.
/// </summary>
[ProtoContract]
public class ReadModelCutEntry
{
    /// <summary>
    /// Gets or sets the read model identifier this entry is for.
    /// </summary>
    [ProtoMember(1)]
    public string ReadModel { get; set; }

    /// <summary>
    /// Gets or sets the outcome for this read model.
    /// </summary>
    [ProtoMember(2)]
    public ReadModelCutOutcome Outcome { get; set; }

    /// <summary>
    /// Gets or sets the read-model schema generation the payload was produced under, when <see cref="Outcome"/> is <see cref="ReadModelCutOutcome.Captured"/>.
    /// </summary>
    [ProtoMember(3)]
    public uint? Generation { get; set; }

    /// <summary>
    /// Gets or sets the lowercase hexadecimal SHA-256 digest of the payload, when <see cref="Outcome"/> is <see cref="ReadModelCutOutcome.Captured"/>.
    /// </summary>
    [ProtoMember(4)]
    public string? Digest { get; set; }

    /// <summary>
    /// Gets or sets a human-readable reason, when <see cref="Outcome"/> is not <see cref="ReadModelCutOutcome.Captured"/>.
    /// </summary>
    [ProtoMember(5)]
    public string? FailureReason { get; set; }
}

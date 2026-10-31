// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>The durable, monotonic compatibility floor for one event store.</summary>
/// <param name="MinimumStorageProtocol">The minimum storage protocol.</param>
/// <param name="MinimumWriterProtocol">The minimum writer protocol.</param>
/// <param name="RequiredCapabilities">Required capabilities, normalized on construction.</param>
/// <param name="Incarnation">The store incarnation.</param>
/// <param name="Revision">The monotonic compare-and-swap token.</param>
/// <param name="AdvancedAt">When the floor was advanced.</param>
/// <param name="AdvancedBy">Who advanced the floor.</param>
/// <param name="Reason">Why the floor was advanced.</param>
public record GenerationFloor(
    GenerationProtocolVersion MinimumStorageProtocol,
    GenerationProtocolVersion MinimumWriterProtocol,
    IReadOnlyList<GenerationCapability> RequiredCapabilities,
    StoreIncarnation Incarnation,
    ulong Revision,
    DateTimeOffset AdvancedAt,
    string AdvancedBy,
    string Reason)
{
    /// <summary>Gets the provider-representable monotonic revision.</summary>
    public ulong Revision { get; init; } = Revision <= long.MaxValue ? Revision : throw new ArgumentOutOfRangeException(nameof(Revision));

    /// <summary>Gets the sorted, distinct, immutable required capability set.</summary>
    public IReadOnlyList<GenerationCapability> RequiredCapabilities { get; } = Array.AsReadOnly(RequiredCapabilities.Distinct().OrderBy(_ => _.Value, StringComparer.Ordinal).ToArray());
}

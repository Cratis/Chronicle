// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>Protocols and capabilities implemented by the executing binary, never inferred from storage.</summary>
/// <param name="Storage">The storage protocol.</param>
/// <param name="Writer">The writer protocol.</param>
/// <param name="Capabilities">Implemented capabilities.</param>
public record GenerationBinarySupport(GenerationProtocolVersion Storage, GenerationProtocolVersion Writer, IReadOnlySet<GenerationCapability> Capabilities)
{
    /// <summary>Gets this bridge binary's support. Adoption and binding are not implemented here.</summary>
    public static GenerationBinarySupport Current { get; } = new(GenerationProtocolVersion.Current, GenerationProtocolVersion.Current, new[] { GenerationCapability.FloorAdmission }.ToFrozenSet());

    /// <summary>Checks every independent floor component.</summary>
    /// <param name="floor">The durable requirement.</param>
    /// <returns>Whether this binary satisfies the floor.</returns>
    public bool Satisfies(GenerationFloor floor) => Storage.IsAtLeast(floor.MinimumStorageProtocol) && Writer.IsAtLeast(floor.MinimumWriterProtocol) && floor.RequiredCapabilities.All(Capabilities.Contains);
}

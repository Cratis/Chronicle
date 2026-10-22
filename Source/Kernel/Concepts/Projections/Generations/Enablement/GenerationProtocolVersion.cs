// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>A generation storage or writer protocol version.</summary>
/// <param name="Value">The version, representable by every storage provider.</param>
public record GenerationProtocolVersion(uint Value) : ConceptAs<uint>(Value <= int.MaxValue ? Value : throw new ArgumentOutOfRangeException(nameof(Value)))
{
    /// <summary>No generation protocol.</summary>
    public static readonly GenerationProtocolVersion None = new(0);

    /// <summary>The bridge protocol implemented by this binary.</summary>
    public static readonly GenerationProtocolVersion Current = new(1);

    /// <summary>Checks protocol ordering.</summary>
    /// <param name="other">The required version.</param>
    /// <returns>Whether this version meets the requirement.</returns>
    public bool IsAtLeast(GenerationProtocolVersion other) => Value >= other.Value;
}

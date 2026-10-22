// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf;

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>The conditional outcome of advancing a generation compatibility floor.</summary>
public sealed class GenerationFloorAdvanceResult : OneOfBase<GenerationFloorAdvanced, GenerationFloorAlreadyAtOrAbove, GenerationFloorRevisionConflict, GenerationFloorWouldLower, GenerationFloorMembershipNotUniform>
{
    GenerationFloorAdvanceResult(OneOf<GenerationFloorAdvanced, GenerationFloorAlreadyAtOrAbove, GenerationFloorRevisionConflict, GenerationFloorWouldLower, GenerationFloorMembershipNotUniform> value) : base(value)
    {
    }

    /// <summary>Converts a GenerationFloorAdvanced outcome.</summary>
    /// <param name="value">The outcome.</param>
    /// <returns>The conditional floor outcome.</returns>
    public static implicit operator GenerationFloorAdvanceResult(GenerationFloorAdvanced value) => new(value);

    /// <summary>Converts a GenerationFloorAlreadyAtOrAbove outcome.</summary>
    /// <param name="value">The outcome.</param>
    /// <returns>The conditional floor outcome.</returns>
    public static implicit operator GenerationFloorAdvanceResult(GenerationFloorAlreadyAtOrAbove value) => new(value);

    /// <summary>Converts a GenerationFloorRevisionConflict outcome.</summary>
    /// <param name="value">The outcome.</param>
    /// <returns>The conditional floor outcome.</returns>
    public static implicit operator GenerationFloorAdvanceResult(GenerationFloorRevisionConflict value) => new(value);

    /// <summary>Converts a GenerationFloorWouldLower outcome.</summary>
    /// <param name="value">The outcome.</param>
    /// <returns>The conditional floor outcome.</returns>
    public static implicit operator GenerationFloorAdvanceResult(GenerationFloorWouldLower value) => new(value);

    /// <summary>Converts a GenerationFloorMembershipNotUniform outcome.</summary>
    /// <param name="value">The outcome.</param>
    /// <returns>The conditional floor outcome.</returns>
    public static implicit operator GenerationFloorAdvanceResult(GenerationFloorMembershipNotUniform value) => new(value);
}

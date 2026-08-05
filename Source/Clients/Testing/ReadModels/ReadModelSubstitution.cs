// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents one substituted layer that the read model under test actually depends on, and what that costs
/// the spec asserting against it.
/// </summary>
/// <remarks>
/// A substitution is not a defect and not a warning about the read model — it says that the part of the
/// behavior named by <see cref="Shape"/> is produced here by something other than what produces it live, so
/// a spec at this tier cannot be the last word on it.
/// </remarks>
/// <param name="Layer">The <see cref="ReadModelSubstitutedLayer"/> that stands in.</param>
/// <param name="Shape">What in the read model reaches that layer.</param>
/// <param name="Consequence">What a spec at this tier therefore does not establish.</param>
public record ReadModelSubstitution(ReadModelSubstitutedLayer Layer, string Shape, string Consequence)
{
    /// <summary>
    /// Returns the substitution as a single sentence naming the shape, the layer and the consequence.
    /// </summary>
    /// <returns>A description of the substitution.</returns>
    public override string ToString() => $"{Shape} reaches {Layer}, which the in-process harness substitutes — {Consequence}";
}

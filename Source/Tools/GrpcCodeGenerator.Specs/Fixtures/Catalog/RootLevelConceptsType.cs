// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Stands in for a type declared with nothing left after skipping and stripping the transparent "Concepts" layer
/// segment - directly under it, with no area segment beneath. Exercises the case that used to produce a dangling
/// trailing dot ("Cratis.Chronicle.Contracts.") no namespace can parse.
/// </summary>
public enum RootLevelConceptsType
{
    /// <summary>Represents the only value.</summary>
    Only = 0,
}

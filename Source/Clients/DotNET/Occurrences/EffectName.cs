// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Names one effect within an occurrence.
/// </summary>
/// <param name="Value">The nonempty, stable effect name.</param>
public record EffectName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Converts an effect name.
    /// </summary>
    /// <param name="value">The name.</param>
    public static implicit operator EffectName(string value) => new(value);
}

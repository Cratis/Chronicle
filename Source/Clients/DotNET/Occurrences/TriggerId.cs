// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Identifies a stable application trigger.
/// </summary>
/// <param name="Value">The stable trigger identifier.</param>
public record TriggerId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Converts a trigger identifier.
    /// </summary>
    /// <param name="value">The identifier.</param>
    public static implicit operator TriggerId(string value) => new(value);
}

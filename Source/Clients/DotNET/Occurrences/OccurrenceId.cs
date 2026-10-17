// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Identifies one durable occurrence.
/// </summary>
/// <param name="Value">The opaque identity.</param>
public record OccurrenceId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Converts an opaque identity to an occurrence id.
    /// </summary>
    /// <param name="value">The identity.</param>
    public static implicit operator OccurrenceId(string value) => new(value);
}

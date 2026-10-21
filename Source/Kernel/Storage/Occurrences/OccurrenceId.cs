// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Represents a durable occurrence identity.
/// </summary>
/// <param name="Value">The opaque identity.</param>
public record OccurrenceId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Converts an identity to its typed form.
    /// </summary>
    /// <param name="value">The identity.</param>
    /// <returns>The typed identity.</returns>
    public static implicit operator OccurrenceId(string value) => new(value);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Represents the revision of the definition a <see cref="MaterializationGeneration"/> is built from - the definition fingerprint.
/// </summary>
/// <param name="Value">Actual value.</param>
public record MaterializationRevision(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets a value representing an unknown revision, as for adopted legacy materializations.
    /// </summary>
    public static readonly MaterializationRevision Unknown = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="MaterializationRevision"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    public static implicit operator MaterializationRevision(string value) => new(value);
}

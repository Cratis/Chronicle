// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Identifies one logical application activation shared by all its instances.
/// </summary>
/// <param name="Value">The orchestrator's stable activation identity.</param>
public record ActivationId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Converts a host-supplied identity.
    /// </summary>
    /// <param name="value">The activation identity.</param>
    public static implicit operator ActivationId(string value) => new(value);
}

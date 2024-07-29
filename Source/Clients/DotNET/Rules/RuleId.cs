// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Rules;

/// <summary>
/// Represents the unique identifier of a projection.
/// </summary>
/// <param name="Value">The value.</param>
public record RuleId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from string to <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator RuleId(string value) => new(value);
}

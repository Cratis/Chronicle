// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Represents a domain specification describing the purpose and context of an event store.
/// </summary>
/// <param name="Value">The domain specification text.</param>
public record DomainSpecification(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets a <see cref="DomainSpecification"/> representing an empty or not set specification.
    /// </summary>
    public static readonly DomainSpecification NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="DomainSpecification"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator DomainSpecification(string value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="DomainSpecification"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="specification">The specification to convert.</param>
    public static implicit operator string(DomainSpecification specification) => specification.Value;
}

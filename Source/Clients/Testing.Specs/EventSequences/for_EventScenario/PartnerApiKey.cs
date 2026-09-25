// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// An operational secret with no data subject - marked [Encrypted], not [PII], because there is nothing here a
/// right-to-erasure request could lawfully be about.
/// </summary>
/// <param name="Value">The API key.</param>
[Encrypted]
public record PartnerApiKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="PartnerApiKey"/>.
    /// </summary>
    /// <param name="value">The API key.</param>
    public static implicit operator PartnerApiKey(string value) => new(value);
}

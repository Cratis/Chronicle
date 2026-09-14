// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// An email address, which is personal data wherever it appears — so the marker sits on the concept rather
/// than on each event that carries one.
/// </summary>
/// <param name="Value">The email address.</param>
[PII]
public record MemberEmailAddress(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="MemberEmailAddress"/>.
    /// </summary>
    /// <param name="value">The email address.</param>
    public static implicit operator MemberEmailAddress(string value) => new(value);
}

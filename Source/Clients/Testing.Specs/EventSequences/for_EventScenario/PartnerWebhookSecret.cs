// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A webhook signing secret shared by every partner integration in the namespace - namespace-scoped rather than
/// subject-scoped, because it is not tied to any one partner's identity.
/// </summary>
/// <param name="Value">The webhook signing secret.</param>
[Encrypted(EncryptionScope.Namespace)]
public record PartnerWebhookSecret(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="PartnerWebhookSecret"/>.
    /// </summary>
    /// <param name="value">The webhook signing secret.</param>
    public static implicit operator PartnerWebhookSecret(string value) => new(value);
}

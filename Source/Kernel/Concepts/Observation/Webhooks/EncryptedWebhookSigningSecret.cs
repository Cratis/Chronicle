// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents the encrypted private material of a webhook signing key.
/// </summary>
/// <param name="Value">The ciphertext produced by the kernel encryption.</param>
public record EncryptedWebhookSigningSecret(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of no secret, used for retired keys.
    /// </summary>
    public static readonly EncryptedWebhookSigningSecret Empty = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="EncryptedWebhookSigningSecret"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator EncryptedWebhookSigningSecret(string value) => new(value);
}

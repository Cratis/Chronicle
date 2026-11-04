// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents the unique identifier of a webhook signing key.
/// </summary>
/// <param name="Value">The actual value.</param>
public record WebhookSigningKeyId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="WebhookSigningKeyId"/>.
    /// </summary>
    public static readonly WebhookSigningKeyId Unspecified = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="WebhookSigningKeyId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator WebhookSigningKeyId(string id) => new(id);

    /// <summary>
    /// Creates a new, unique <see cref="WebhookSigningKeyId"/>.
    /// </summary>
    /// <returns>A new <see cref="WebhookSigningKeyId"/>.</returns>
    public static WebhookSigningKeyId New() => new(Guid.NewGuid().ToString("N"));
}

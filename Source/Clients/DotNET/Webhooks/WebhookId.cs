// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Represents the unique identifier of a webhook.
/// </summary>
/// <param name="Value">The actual value.</param>
public record WebhookId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="WebhookId"/>.
    /// </summary>
    public static readonly WebhookId Unspecified = ObserverId.Unspecified;

    /// <summary>
    /// Implicitly convert from a string to <see cref="WebhookId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator WebhookId(string id) => new(id);

    /// <summary>
    /// Implicitly convert from <see cref="WebhookId"/> to <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id"><see cref="WebhookId"/> to convert from.</param>
    public static implicit operator ObserverId(WebhookId id) => new(id.Value);

    /// <summary>
    /// Implicitly convert from <see cref="ObserverId"/> to <see cref="WebhookId"/>.
    /// </summary>
    /// <param name="id"><see cref="ObserverId"/> to convert from.</param>
    public static implicit operator WebhookId(ObserverId id) => new(id.Value);
}

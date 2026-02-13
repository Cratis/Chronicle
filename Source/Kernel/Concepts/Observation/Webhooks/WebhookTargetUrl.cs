// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents the target URL for a webhook.
/// </summary>
/// <param name="Value">The url string.</param>
public record WebhookTargetUrl(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator WebhookTargetUrl(string value) => new(value);
    public static implicit operator string(WebhookTargetUrl url) => url.Value;
}
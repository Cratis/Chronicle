// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents the target of a webhook.
/// </summary>
/// <param name="Url">The <see cref="WebhookTargetUrl"/>.</param>
/// <param name="Authentication">The <see cref="AuthenticationType"/>.</param>
/// <param name="Username">The optional username.</param>
/// <param name="Password">The optional password.</param>
/// <param name="BearerToken">The optional bearer token.</param>
/// <param name="Headers">The headers.</param>
public record WebhookTarget(
    WebhookTargetUrl Url,
    AuthenticationType Authentication,
    string? Username,
    string? Password,
    string? BearerToken,
    IReadOnlyDictionary<string, string> Headers);
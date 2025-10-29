// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Observation.Webhooks;

/// <summary>
/// Represents the target of a webhook.
/// </summary>
/// <param name="Url">The target url.</param>
/// <param name="Authentication">The <see cref="AuthenticationType"/>.</param>
/// <param name="Username">The optional username.</param>
/// <param name="Password">The optional password.</param>
/// <param name="BearerToken">The optional bearer token.</param>
/// <param name="Headers">The headers.</param>
public record WebhookTarget(
    string Url,
    AuthenticationType Authentication,
    string? Username,
    string? Password,
    string? BearerToken,
#pragma warning disable MA0016
    Dictionary<string, string> Headers);
#pragma warning restore MA0016

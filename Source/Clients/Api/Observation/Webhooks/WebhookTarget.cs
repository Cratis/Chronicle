// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf.Types;

namespace Cratis.Chronicle.Api.Observation.Webhooks;

/// <summary>
/// Represents the target of a webhook.
/// </summary>
/// <param name="Url">The target url.</param>
/// <param name="Authorization">The authorization method.</param>
/// <param name="Headers">The headers.</param>
public record WebhookTarget(
    string Url,
    OneOf.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, None> Authorization,
#pragma warning disable MA0016
    Dictionary<string, string> Headers);
#pragma warning restore MA0016

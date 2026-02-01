// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Represents the target of a webhook.
/// </summary>
/// <param name="Url">The <see cref="WebhookTargetUrl"/>.</param>
/// <param name="Authorization">The authorization method.</param>
/// <param name="Headers">The headers.</param>
public record WebhookTarget(
    WebhookTargetUrl Url,
    OneOf.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, OneOf.Types.None> Authorization,
    IReadOnlyDictionary<string, string> Headers);

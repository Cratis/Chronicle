// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents the event for OAuth authorization being set for a webhook.
/// </summary>
/// <param name="Authority">The OAuth authority.</param>
/// <param name="ClientId">The OAuth client ID.</param>
/// <param name="ClientSecret">The OAuth client secret.</param>
[EventType]
public record OAuthAuthorizationSetForWebhook(Authority Authority, ClientId ClientId, ClientSecret ClientSecret);

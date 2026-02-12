// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents the event for bearer token authorization being set for a webhook.
/// </summary>
/// <param name="Token">The bearer token.</param>
[EventType]
public record BearerTokenAuthorizationSetForWebhook(Token Token);

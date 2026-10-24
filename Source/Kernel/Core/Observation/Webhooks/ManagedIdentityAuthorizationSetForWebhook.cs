// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents managed identity authorization being set for a webhook.
/// </summary>
/// <param name="ClientId">The client identifier, or empty for the system-assigned identity.</param>
/// <param name="Scope">The scope requested for the receiver.</param>
[EventType, AllEventStores]
public record ManagedIdentityAuthorizationSetForWebhook(ManagedIdentityClientId ClientId, OAuthScope Scope);

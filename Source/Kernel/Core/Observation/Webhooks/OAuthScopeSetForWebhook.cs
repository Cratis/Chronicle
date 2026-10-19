// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Records the optional scope requested when a webhook uses OAuth client credentials.
/// </summary>
/// <param name="Scope">The OAuth scope.</param>
[EventType, AllEventStores]
public record OAuthScopeSetForWebhook(OAuthScope Scope);

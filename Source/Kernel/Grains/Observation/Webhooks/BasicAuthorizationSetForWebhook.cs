// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents the event for basic authorization being set for a webhook.
/// </summary>
/// <param name="Username">The username.</param>
/// <param name="Password">The password.</param>
[EventType]
public record BasicAuthorizationSetForWebhook(Username Username, Password Password);

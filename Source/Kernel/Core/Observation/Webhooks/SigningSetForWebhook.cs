// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Records a change to a webhook's signing policy.
/// </summary>
/// <param name="Signing">The signing policy.</param>
[EventType, AllEventStores]
public record SigningSetForWebhook(WebhookSigningMode Signing);

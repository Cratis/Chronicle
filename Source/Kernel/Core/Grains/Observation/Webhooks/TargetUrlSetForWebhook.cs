// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents the event for target URL being set for a webhook.
/// </summary>
/// <param name="TargetUrl">The target URL.</param>
[EventType]
public record TargetUrlSetForWebhook(WebhookTargetUrl TargetUrl);

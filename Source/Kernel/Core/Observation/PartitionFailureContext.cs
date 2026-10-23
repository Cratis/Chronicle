// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Carries internal recovery policy and optional original delivery context for a failed attempt.
/// </summary>
/// <param name="Policy">The webhook recovery policy; the default retries normally.</param>
/// <param name="DeliveryFence">The optional original delivery identity and deadline.</param>
public readonly record struct PartitionFailureContext(
    WebhookDeliveryFailurePolicy Policy = WebhookDeliveryFailurePolicy.Retry,
    DeliveryFenceContext? DeliveryFence = null);

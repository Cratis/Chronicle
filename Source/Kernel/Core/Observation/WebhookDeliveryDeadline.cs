// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines the timeout and clock for a webhook delivery.
/// </summary>
/// <param name="Timeout">The delivery timeout.</param>
/// <param name="Clock">The clock driving the timeout.</param>
internal readonly record struct WebhookDeliveryDeadline(TimeSpan Timeout, TimeProvider Clock);

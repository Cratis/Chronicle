// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Kernel-only webhook recovery decisions, separate from the failure kinds exposed over the wire.
/// </summary>
public enum WebhookDeliveryFailurePolicy
{
    /// <summary>
    /// Use the observer's configured retry budget and backoff.
    /// </summary>
    Retry = 0,

    /// <summary>
    /// Pause the observer until its delivery configuration is corrected.
    /// </summary>
    PauseObserver = 1,

    /// <summary>
    /// Park the affected partition immediately for manual intervention.
    /// </summary>
    ParkPartition = 2,

    /// <summary>The endpoint is permanently gone; stop the whole webhook.</summary>
    RetireWebhook = 3
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Defines the format used to deliver webhook events.
/// </summary>
public enum WebhookDeliveryFormat
{
    /// <summary>
    /// Deliver events in the legacy format.
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// Deliver events using delivery envelope version 1.
    /// </summary>
    EnvelopeV1 = 1
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// Holds the unsigned envelope delivery headers.
/// </summary>
public static class DeliveryHeaders
{
    /// <summary>
    /// The stable delivery identifier.
    /// </summary>
    public const string Id = "webhook-id";

    /// <summary>
    /// The UTC execution deadline.
    /// </summary>
    public const string Deadline = "Cratis-Delivery-Deadline";

    /// <summary>
    /// The W3C trace context.
    /// </summary>
    public const string TraceParent = "traceparent";
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// A delivery outcome, retaining partial progress and distinguishing a deadline from a failure.
/// </summary>
/// <param name="LastHandled">The last successfully delivered event, or unavailable.</param>
/// <param name="Error">The error, when delivery did not complete.</param>
/// <param name="Deferral">The durable deadline, when delivery should wait.</param>
public record WebhookDeliveryResult(EventSequenceNumber LastHandled, Exception? Error = null, PartitionDeferral? Deferral = null)
{
    /// <summary>
    /// Gets whether every event was delivered.
    /// </summary>
    public bool IsSuccess => Error is null && Deferral is null;

    /// <summary>
    /// Gets the failure classification used by the observer's durable retry policy.
    /// </summary>
    public WebhookDeliveryFailurePolicy FailurePolicy { get; init; } = WebhookDeliveryFailurePolicy.Retry;

    /// <summary>Gets the bounded HTTP failure status, without target or response content.</summary>
    public int? HttpStatusCode { get; init; }
}

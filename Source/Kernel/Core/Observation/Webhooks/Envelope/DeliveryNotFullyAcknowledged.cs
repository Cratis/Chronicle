// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// The exception that is thrown when a receiver reports a failure or acknowledges only a prefix.
/// </summary>
/// <param name="targetUrl">The URL of the webhook target.</param>
/// <param name="lastAcknowledged">The last acknowledged observation, or Unavailable.</param>
/// <param name="receiverMessages">The receiver messages safe to retain.</param>
/// <param name="receiverStackTrace">The receiver stack trace safe to retain.</param>
public class DeliveryNotFullyAcknowledged(WebhookTargetUrl targetUrl, EventSequenceNumber lastAcknowledged, IEnumerable<string> receiverMessages, string receiverStackTrace)
    : Exception($"Webhook delivery to '{targetUrl}' was not fully acknowledged; last acknowledged: {(lastAcknowledged.IsUnavailable ? "nothing" : lastAcknowledged.ToString())}. {string.Join("; ", receiverMessages)}")
{
    /// <summary>
    /// Gets the last acknowledged observation.
    /// </summary>
    public EventSequenceNumber LastAcknowledged { get; } = lastAcknowledged;

    /// <summary>
    /// Gets the receiver messages safe to retain.
    /// </summary>
    public IEnumerable<string> ReceiverMessages { get; } = receiverMessages;

    /// <inheritdoc/>
    public override string? StackTrace => string.IsNullOrEmpty(receiverStackTrace) ? base.StackTrace : receiverStackTrace;
}

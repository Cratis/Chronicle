// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Defines signing of the exact bytes of a webhook delivery.
/// </summary>
public interface IWebhookDeliverySigner
{
    /// <summary>
    /// Signs a delivery with the event store's valid signing keys.
    /// </summary>
    /// <param name="eventStore">The event store owning the keys.</param>
    /// <param name="messageId">The deterministic message identifier.</param>
    /// <param name="unixTimestamp">The attempt time in Unix seconds.</param>
    /// <param name="body">The exact outgoing body bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The delivery header values.</returns>
    /// <exception cref="WebhookSigningUnavailable">Signing could not complete safely.</exception>
    Task<WebhookDeliverySignature> Sign(EventStoreName eventStore, string messageId, long unixTimestamp, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// Builds and serializes delivery-v1 envelopes.
/// </summary>
public interface IDeliveryEnvelopeWriter
{
    /// <summary>
    /// Builds one live partition batch.
    /// </summary>
    /// <param name="request">The delivery inputs.</param>
    /// <returns>The events envelope.</returns>
    DeliveryEnvelope ForEvents(HttpDeliveryRequest request);

    /// <summary>
    /// Builds a control envelope without enabling control dispatch.
    /// </summary>
    /// <param name="kind">The control kind.</param>
    /// <param name="request">The observer identity inputs.</param>
    /// <returns>The control envelope.</returns>
    DeliveryEnvelope Control(DeliveryKind kind, HttpDeliveryRequest request);

    /// <summary>
    /// Serializes the exact payload bytes for a future signer and HTTP content.
    /// </summary>
    /// <param name="envelope">The envelope.</param>
    /// <returns>The UTF-8 JSON bytes.</returns>
    byte[] Serialize(DeliveryEnvelope envelope);
}

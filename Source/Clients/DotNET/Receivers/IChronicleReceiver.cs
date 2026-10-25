// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Receives deliveries independently of the hosting transport.
/// </summary>
internal interface IChronicleReceiver
{
    /// <summary>
    /// Decodes and dispatches a delivery.
    /// </summary>
    /// <param name="request">The transport request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The delivery outcome.</returns>
    Task<DeliveryOutcome> Receive(DeliveryRequest request, CancellationToken cancellationToken);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Decodes a transport request into a delivery.
/// </summary>
internal interface IDeliveryDecoder
{
    /// <summary>
    /// Decodes the incoming body.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The decoded delivery.</returns>
    Task<Delivery> Decode(DeliveryRequest request, CancellationToken cancellationToken);
}

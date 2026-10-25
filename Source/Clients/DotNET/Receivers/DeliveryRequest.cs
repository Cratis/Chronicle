// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Represents an incoming transport body and an optional transport-supplied target.
/// </summary>
/// <param name="Body">The request body, owned by the caller.</param>
/// <param name="Target">The transport target, or null when the transport could not resolve it.</param>
internal record DeliveryRequest(Stream Body, DeliveryTarget? Target)
{
    /// <summary>
    /// Gets a value indicating whether an internal transport verified the delivery signature.
    /// </summary>
    internal bool SignatureVerified { get; init; }
}

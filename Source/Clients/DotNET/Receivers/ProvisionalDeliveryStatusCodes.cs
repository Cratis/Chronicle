// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Provides provisional status codes aligned with today's WebhookMediator handling.
/// Replaced when decision 0020 is accepted; this mapping does not define the permanent retry contract.
/// </summary>
internal sealed class ProvisionalDeliveryStatusCodes : IDeliveryStatusCodes
{
    /// <inheritdoc/>
    public int For(DeliveryOutcome outcome) => outcome.Status switch
    {
        DeliveryStatus.Succeeded => 200,
        DeliveryStatus.Failed => 500,
        DeliveryStatus.Cancelled => 503,
        DeliveryStatus.UnknownObserver => 404,
        DeliveryStatus.Malformed => 400,
        DeliveryStatus.TooLarge => 413,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };
}

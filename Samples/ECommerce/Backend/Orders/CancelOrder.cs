// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Orders;

/// <summary>
/// Command for cancelling an existing order.
/// Uses <see cref="OneOf{T0,T1}"/> to return either a confirmation or an error.
/// </summary>
/// <param name="OrderId">The identifier of the order to cancel.</param>
/// <param name="Reason">The reason for cancellation.</param>
[Command]
[BelongsTo("OrderService")]
public record CancelOrder(OrderId OrderId, string Reason)
{
    /// <summary>
    /// Handles the command and returns either a confirmation or an error.
    /// </summary>
    /// <returns>A <see cref="OneOf{T0,T1}"/> containing the result or an error.</returns>
    internal Task<OneOf<CancellationConfirmation, CancellationError>> Handle()
    {
        Console.WriteLine($"[Orders] Cancelling order {OrderId}. Reason: {Reason}");

        if (OrderId == OrderId.NotSet)
        {
            return Task.FromResult<OneOf<CancellationConfirmation, CancellationError>>(CancellationError.OrderNotFound);
        }

        var cancelledAt = DateTimeOffset.UtcNow;
        return Task.FromResult<OneOf<CancellationConfirmation, CancellationError>>(new CancellationConfirmation(cancelledAt));
    }
}

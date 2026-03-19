// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Orders;

/// <summary>
/// Represents the result of a successful order cancellation.
/// </summary>
/// <param name="CancelledAt">The timestamp when the order was cancelled.</param>
public record CancellationConfirmation(DateTimeOffset CancelledAt);

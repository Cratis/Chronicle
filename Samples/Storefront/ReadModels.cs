// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

namespace Samples.Storefront;

/// <summary>
/// Represents where an order has got to.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Placed and on its way through the warehouse.
    /// </summary>
    Placed = 0,

    /// <summary>
    /// Pulled out for somebody to look at before it ships.
    /// </summary>
    HeldForReview = 1,

    /// <summary>
    /// Looked at and allowed to continue.
    /// </summary>
    Released = 2,

    /// <summary>
    /// Stopped for good.
    /// </summary>
    Cancelled = 3
}

/// <summary>
/// Represents how far a shipment has got out of the door.
/// </summary>
public enum ShipmentStatus
{
    /// <summary>
    /// Collected from the shelves.
    /// </summary>
    Picked = 0,

    /// <summary>
    /// Boxed up ready to go.
    /// </summary>
    Packed = 1,

    /// <summary>
    /// Handed to a carrier.
    /// </summary>
    Dispatched = 2,

    /// <summary>
    /// Confirmed as arrived.
    /// </summary>
    Delivered = 3
}

/// <summary>
/// The current state of a customer order.
/// </summary>
/// <param name="Customer">Who placed it.</param>
/// <param name="Total">What it came to.</param>
/// <param name="Items">How many items were in it.</param>
/// <param name="Status">Where it has got to.</param>
/// <param name="Reason">Why it was held or cancelled.</param>
/// <remarks>
/// <see cref="Status"/> carries an initial value because an order genuinely starts out placed - that is what the
/// event creating it means, not a stand-in for a projection that has not run.
/// </remarks>
[FromEvent<OrderPlaced>]
[FromEvent<OrderHeldForReview>]
[FromEvent<OrderReleased>]
[FromEvent<OrderCancelled>]
public record Order(
    PersonName Customer,
    Money Total,
    Quantity Items,
    [property: SetValue<OrderHeldForReview>(OrderStatus.HeldForReview)]
    [property: SetValue<OrderReleased>(OrderStatus.Released)]
    [property: SetValue<OrderCancelled>(OrderStatus.Cancelled)]
    OrderStatus Status,
    Reason Reason);

/// <summary>
/// The current state of a shipment.
/// </summary>
/// <param name="Picker">Who picked it.</param>
/// <param name="Items">How many items were picked.</param>
/// <param name="Packages">How many packages it came to.</param>
/// <param name="Carrier">Who is carrying it.</param>
/// <param name="Status">How far it has got.</param>
[FromEvent<OrderPicked>]
[FromEvent<ShipmentPacked>]
[FromEvent<ShipmentDispatched>]
[FromEvent<ShipmentDelivered>]
public record Shipment(
    PersonName Picker,
    Quantity Items,
    Quantity Packages,
    Carrier Carrier,
    [property: SetValue<ShipmentPacked>(ShipmentStatus.Packed)]
    [property: SetValue<ShipmentDispatched>(ShipmentStatus.Dispatched)]
    [property: SetValue<ShipmentDelivered>(ShipmentStatus.Delivered)]
    ShipmentStatus Status);

/// <summary>
/// The current state of a product in the catalog.
/// </summary>
/// <param name="Price">What it currently costs.</param>
/// <param name="Stock">How much of it is on hand.</param>
/// <param name="TimesRepriced">How many times its price has moved.</param>
/// <remarks>
/// Stock accumulates from two different events - what a buyer orders in and what the overnight run tops up - which
/// is the sort of thing worth having real data behind when demonstrating the projection query editor.
/// </remarks>
[FromEvent<ProductRestocked>]
[FromEvent<PriceChanged>]
[FromEvent<InventoryReplenished>]
public record Product(
    [property: SetFrom<PriceChanged>(nameof(PriceChanged.NewPrice))]
    [property: NoAutoMap]
    Money Price,
    [property: AddFrom<ProductRestocked>(nameof(ProductRestocked.Quantity))]
    [property: AddFrom<InventoryReplenished>(nameof(InventoryReplenished.Quantity))]
    [property: NoAutoMap]
    Quantity Stock,
    [property: Count<PriceChanged>]
    int TimesRepriced);

/// <summary>
/// What each member of staff has got through.
/// </summary>
/// <param name="Picked">How many orders they picked.</param>
/// <param name="Packed">How many shipments they packed.</param>
/// <param name="TicketsAnswered">How many support tickets they answered.</param>
/// <param name="ReturnsApproved">How many returns they approved.</param>
/// <remarks>
/// Keyed by the person rather than by the thing they acted on, so the projection query editor has an aggregate to
/// demonstrate rather than only one-row-per-event-source models. Each count comes from the event that names them,
/// which is why a picker's row stays empty in the columns belonging to somebody else's job - the shape of the
/// working day showing up in a read model as well as in the patterns.
/// </remarks>
[FromEvent<OrderPicked>(nameof(OrderPicked.Picker))]
[FromEvent<ShipmentPacked>(nameof(ShipmentPacked.Packer))]
[FromEvent<TicketAnswered>(nameof(TicketAnswered.Agent))]
[FromEvent<ReturnApproved>(nameof(ReturnApproved.Agent))]
public record StaffActivity(
    [property: Count<OrderPicked>]
    int Picked,
    [property: Count<ShipmentPacked>]
    int Packed,
    [property: Count<TicketAnswered>]
    int TicketsAnswered,
    [property: Count<ReturnApproved>]
    int ReturnsApproved);

/// <summary>
/// Lets the sample history be generated exactly once.
/// </summary>
/// <remarks>
/// The generator appends its marker before anything else, so a second run is turned away by this constraint and
/// leaves the store as it found it rather than laying a duplicate history on top of the first.
/// </remarks>
public class UniqueSampleHistory : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<SampleHistoryGenerated>("This store already holds the sample history.");
}

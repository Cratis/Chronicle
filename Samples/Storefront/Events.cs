// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Samples.Storefront;

/// <summary>
/// Emitted when a customer places an order.
/// </summary>
/// <param name="Customer">Who placed it.</param>
/// <param name="Total">What it came to.</param>
/// <param name="Items">How many items were in it.</param>
[EventType]
public record OrderPlaced(PersonName Customer, Money Total, Quantity Items);

/// <summary>
/// Emitted when an order is pulled out of the flow for someone to look at before it ships.
/// </summary>
/// <param name="Analyst">Who held it.</param>
/// <param name="Reason">What made it worth a second look.</param>
[EventType]
public record OrderHeldForReview(PersonName Analyst, Reason Reason);

/// <summary>
/// Emitted when a held order is judged fine and allowed to continue.
/// </summary>
/// <param name="Analyst">Who released it.</param>
[EventType]
public record OrderReleased(PersonName Analyst);

/// <summary>
/// Emitted when an order is stopped for good.
/// </summary>
/// <param name="Analyst">Who cancelled it.</param>
/// <param name="Reason">Why it was cancelled.</param>
[EventType]
public record OrderCancelled(PersonName Analyst, Reason Reason);

/// <summary>
/// Emitted when the items for an order have been collected from the shelves.
/// </summary>
/// <param name="Picker">Who picked it.</param>
/// <param name="Items">How many items were picked.</param>
[EventType]
public record OrderPicked(PersonName Picker, Quantity Items);

/// <summary>
/// Emitted when picked items have been boxed up ready to go out.
/// </summary>
/// <param name="Packer">Who packed it.</param>
/// <param name="Packages">How many packages it came to.</param>
[EventType]
public record ShipmentPacked(PersonName Packer, Quantity Packages);

/// <summary>
/// Emitted when a packed shipment is handed over to a carrier.
/// </summary>
/// <param name="Dispatcher">Who sent it out.</param>
/// <param name="Carrier">Who is carrying it.</param>
[EventType]
public record ShipmentDispatched(PersonName Dispatcher, Carrier Carrier);

/// <summary>
/// Emitted when a carrier confirms a shipment reached the customer.
/// </summary>
/// <param name="Carrier">Who carried it.</param>
[EventType]
public record ShipmentDelivered(Carrier Carrier);

/// <summary>
/// Emitted when a customer asks to send something back.
/// </summary>
/// <param name="Customer">Who asked.</param>
/// <param name="Reason">Why they want to return it.</param>
[EventType]
public record ReturnRequested(PersonName Customer, Reason Reason);

/// <summary>
/// Emitted when a return is accepted and the customer gets their money back.
/// </summary>
/// <param name="Agent">Who approved it.</param>
/// <param name="Refund">What was refunded.</param>
[EventType]
public record ReturnApproved(PersonName Agent, Money Refund);

/// <summary>
/// Emitted when a return is turned down.
/// </summary>
/// <param name="Agent">Who turned it down.</param>
/// <param name="Reason">Why it was turned down.</param>
[EventType]
public record ReturnRejected(PersonName Agent, Reason Reason);

/// <summary>
/// Emitted when a customer opens a support ticket.
/// </summary>
/// <param name="Customer">Who raised it.</param>
/// <param name="Topic">What it is about.</param>
[EventType]
public record TicketRaised(PersonName Customer, TicketTopic Topic);

/// <summary>
/// Emitted when somebody answers a support ticket.
/// </summary>
/// <param name="Agent">Who answered it.</param>
[EventType]
public record TicketAnswered(PersonName Agent);

/// <summary>
/// Emitted when a support ticket is handed to somebody better placed to deal with it.
/// </summary>
/// <param name="Agent">Who passed it on.</param>
/// <param name="EscalatedTo">Who it went to.</param>
[EventType]
public record TicketEscalated(PersonName Agent, PersonName EscalatedTo);

/// <summary>
/// Emitted when more of a product is ordered in.
/// </summary>
/// <param name="Buyer">Who ordered it in.</param>
/// <param name="Quantity">How much was ordered.</param>
[EventType]
public record ProductRestocked(PersonName Buyer, Quantity Quantity);

/// <summary>
/// Emitted when a product's price changes.
/// </summary>
/// <param name="NewPrice">What it now costs.</param>
[EventType]
public record PriceChanged(Money NewPrice);

/// <summary>
/// Emitted when the overnight run tops a product's stock back up.
/// </summary>
/// <param name="Quantity">How much was added.</param>
[EventType]
public record InventoryReplenished(Quantity Quantity);

/// <summary>
/// Emitted once when the sample history has been generated.
/// </summary>
/// <param name="Seed">The seed the run used, so a store says which history it holds.</param>
/// <remarks>
/// This is what makes the generator safe to re-run. A uniqueness constraint means the store accepts it once, so a
/// second run is turned away before it appends anything rather than laying a duplicate history on top of the first.
/// </remarks>
[EventType]
public record SampleHistoryGenerated(Quantity Seed);

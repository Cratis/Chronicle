// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.Storefront;

/// <summary>
/// Works out the events one piece of work produces.
/// </summary>
/// <remarks>
/// Each activity writes a short chain rather than a single event, because what the miner reads as "caused by" only
/// exists when one command genuinely led to another. The work somebody does is nearly always downstream of
/// something a customer did, and that is the shape recorded here.
/// </remarks>
public static class ActivityWriter
{
    /// <summary>
    /// Writes one occurrence of an activity.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> being carried out.</param>
    /// <param name="actor">The <see cref="Actor"/> carrying it out.</param>
    /// <param name="at">When they did it.</param>
    /// <param name="index">The occurrence's position in the run, which determines the identities it uses.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The events the activity produces.</returns>
    public static IEnumerable<PlannedEvent> Plan(Activity activity, Actor actor, DateTimeOffset at, int index, Random random) => activity switch
    {
        Activity.Fulfilment => Fulfil(actor, at, index, random),
        Activity.Dispatch => Dispatch(actor, at, index, random),
        Activity.SupportReply => Support(actor, at, index, random),
        Activity.ReturnDecision => Returns(actor, at, index, random),
        Activity.Restocking => Restock(actor, at, random),
        Activity.Repricing => Reprice(actor, at, random),
        Activity.FraudReview => Review(actor, at, index, random),
        _ => Replenish(actor, at, random)
    };

    static Actor CustomerFrom(Random random) => Workforce.Customers[random.Next(Workforce.Customers.Length)];

    static Reason ReasonFrom(Random random) => Workforce.Reasons[random.Next(Workforce.Reasons.Length)];

    static Money AmountFrom(Random random) => new(Math.Round((decimal)(random.NextDouble() * 900) + 15, 2));

    static ProductId ProductFrom(Random random) => Catalog.Products[random.Next(Catalog.Products.Length)];

    /// <summary>
    /// A customer's order, collected from the shelves and boxed up.
    /// </summary>
    /// <param name="actor">The <see cref="Actor"/> carrying it out.</param>
    /// <param name="at">When they did it.</param>
    /// <param name="index">The occurrence's position in the run.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The events the activity produces.</returns>
    static IEnumerable<PlannedEvent> Fulfil(Actor actor, DateTimeOffset at, int index, Random random)
    {
        var customer = CustomerFrom(random);
        var items = new Quantity(random.Next(1, 9));
        OrderId orderId = Identities.For(index, Identities.OrderMarker);
        ShipmentId shipmentId = Identities.For(index, Identities.ShipmentMarker);

        // The order came in the evening before; the warehouse gets to it the next morning.
        yield return new PlannedEvent(orderId, new OrderPlaced(customer.Name, AmountFrom(random), items), customer, at.AddHours(-random.Next(9, 14)), AggregateType.Order, Commands.PlaceOrder);
        yield return new PlannedEvent(shipmentId, new OrderPicked(actor.Name, items), actor, at, AggregateType.Shipment, Commands.PickOrder, Commands.PlaceOrder);
        yield return new PlannedEvent(shipmentId, new ShipmentPacked(actor.Name, new Quantity(random.Next(1, 3))), actor, at.AddMinutes(random.Next(2, 9)), AggregateType.Shipment, Commands.PackShipment, Commands.PickOrder);
    }

    /// <summary>
    /// A packed shipment handed to a carrier, and the carrier reporting back.
    /// </summary>
    /// <param name="actor">The <see cref="Actor"/> carrying it out.</param>
    /// <param name="at">When they did it.</param>
    /// <param name="index">The occurrence's position in the run.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The events the activity produces.</returns>
    static IEnumerable<PlannedEvent> Dispatch(Actor actor, DateTimeOffset at, int index, Random random)
    {
        var carrier = Workforce.Carriers[random.Next(Workforce.Carriers.Length)];
        ShipmentId shipmentId = Identities.For(index + 500_000, Identities.ShipmentMarker);

        // Packed earlier the same morning by the person whose job that is, so the chain leads somewhere real.
        yield return new PlannedEvent(shipmentId, new ShipmentPacked(Workforce.Maya.Name, new Quantity(random.Next(1, 3))), Workforce.Maya, at.AddMinutes(-random.Next(45, 120)), AggregateType.Shipment, Commands.PackShipment);
        yield return new PlannedEvent(shipmentId, new ShipmentDispatched(actor.Name, carrier), actor, at, AggregateType.Shipment, Commands.DispatchShipment, Commands.PackShipment);

        // Carriers report back in the evening, a couple of days on - the overnight run's second habit.
        var delivered = at.AddDays(2);
        var deliveredAt = new DateTimeOffset(delivered.Year, delivered.Month, delivered.Day, random.Next(17, 21), random.Next(0, 50), 0, TimeSpan.Zero);
        yield return new PlannedEvent(shipmentId, new ShipmentDelivered(carrier), Workforce.Overnight, deliveredAt, AggregateType.Shipment, Commands.ConfirmDelivery, Commands.DispatchShipment);
    }

    /// <summary>
    /// A support ticket, answered or handed on.
    /// </summary>
    /// <param name="actor">The <see cref="Actor"/> carrying it out.</param>
    /// <param name="at">When they did it.</param>
    /// <param name="index">The occurrence's position in the run.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The events the activity produces.</returns>
    static IEnumerable<PlannedEvent> Support(Actor actor, DateTimeOffset at, int index, Random random)
    {
        var customer = CustomerFrom(random);
        var topic = Workforce.Topics[random.Next(Workforce.Topics.Length)];
        TicketId ticketId = Identities.For(index, Identities.TicketMarker);

        yield return new PlannedEvent(ticketId, new TicketRaised(customer.Name, topic), customer, at.AddHours(-random.Next(2, 20)), AggregateType.SupportTicket, Commands.RaiseTicket);

        // Most tickets get answered where they land; the awkward ones go to whoever is covering the floor.
        if (random.Next(100) < 20)
        {
            yield return new PlannedEvent(ticketId, new TicketEscalated(actor.Name, Workforce.Tobias.Name), actor, at, AggregateType.SupportTicket, Commands.EscalateTicket, Commands.RaiseTicket);
            yield break;
        }

        yield return new PlannedEvent(ticketId, new TicketAnswered(actor.Name), actor, at, AggregateType.SupportTicket, Commands.AnswerTicket, Commands.RaiseTicket);
    }

    /// <summary>
    /// A customer's return, accepted or turned down.
    /// </summary>
    /// <param name="actor">The <see cref="Actor"/> carrying it out.</param>
    /// <param name="at">When they did it.</param>
    /// <param name="index">The occurrence's position in the run.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The events the activity produces.</returns>
    static IEnumerable<PlannedEvent> Returns(Actor actor, DateTimeOffset at, int index, Random random)
    {
        var customer = CustomerFrom(random);
        ReturnId returnId = Identities.For(index, Identities.ReturnMarker);

        yield return new PlannedEvent(returnId, new ReturnRequested(customer.Name, ReasonFrom(random)), customer, at.AddDays(-random.Next(1, 4)), AggregateType.Return, Commands.RequestReturn);

        if (random.Next(100) < 70)
        {
            yield return new PlannedEvent(returnId, new ReturnApproved(actor.Name, AmountFrom(random)), actor, at, AggregateType.Return, Commands.ApproveReturn, Commands.RequestReturn);
            yield break;
        }

        yield return new PlannedEvent(returnId, new ReturnRejected(actor.Name, ReasonFrom(random)), actor, at, AggregateType.Return, Commands.RejectReturn, Commands.RequestReturn);
    }

    /// <summary>
    /// More of a product ordered in.
    /// </summary>
    /// <param name="actor">The <see cref="Actor"/> carrying it out.</param>
    /// <param name="at">When they did it.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The events the activity produces.</returns>
    static IEnumerable<PlannedEvent> Restock(Actor actor, DateTimeOffset at, Random random)
    {
        yield return new PlannedEvent(ProductFrom(random), new ProductRestocked(actor.Name, new Quantity(random.Next(20, 200))), actor, at, AggregateType.Product, Commands.RestockProduct);
    }

    /// <summary>
    /// A product's price moved.
    /// </summary>
    /// <param name="actor">The <see cref="Actor"/> carrying it out.</param>
    /// <param name="at">When they did it.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The events the activity produces.</returns>
    static IEnumerable<PlannedEvent> Reprice(Actor actor, DateTimeOffset at, Random random)
    {
        yield return new PlannedEvent(ProductFrom(random), new PriceChanged(AmountFrom(random)), actor, at, AggregateType.Product, Commands.ChangePrice);
    }

    /// <summary>
    /// An order pulled out for a look, then let through or stopped.
    /// </summary>
    /// <param name="actor">The <see cref="Actor"/> carrying it out.</param>
    /// <param name="at">When they did it.</param>
    /// <param name="index">The occurrence's position in the run.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The events the activity produces.</returns>
    static IEnumerable<PlannedEvent> Review(Actor actor, DateTimeOffset at, int index, Random random)
    {
        var customer = CustomerFrom(random);
        OrderId orderId = Identities.For(index + 500_000, Identities.OrderMarker);

        yield return new PlannedEvent(orderId, new OrderPlaced(customer.Name, AmountFrom(random), new Quantity(random.Next(1, 12))), customer, at.AddHours(-random.Next(3, 9)), AggregateType.Order, Commands.PlaceOrder);
        yield return new PlannedEvent(orderId, new OrderHeldForReview(actor.Name, ReasonFrom(random)), actor, at, AggregateType.Order, Commands.HoldOrderForReview, Commands.PlaceOrder);

        var decidedAt = at.AddMinutes(random.Next(3, 9));
        if (random.Next(100) < 75)
        {
            yield return new PlannedEvent(orderId, new OrderReleased(actor.Name), actor, decidedAt, AggregateType.Order, Commands.ReleaseOrder, Commands.HoldOrderForReview);
            yield break;
        }

        yield return new PlannedEvent(orderId, new OrderCancelled(actor.Name, ReasonFrom(random)), actor, decidedAt, AggregateType.Order, Commands.CancelOrder, Commands.HoldOrderForReview);
    }

    /// <summary>
    /// The overnight run topping stock back up.
    /// </summary>
    /// <param name="actor">The <see cref="Actor"/> carrying it out.</param>
    /// <param name="at">When they did it.</param>
    /// <param name="random">The seeded <see cref="Random"/> the run varies through.</param>
    /// <returns>The events the activity produces.</returns>
    static IEnumerable<PlannedEvent> Replenish(Actor actor, DateTimeOffset at, Random random)
    {
        foreach (var product in Catalog.Products.OrderBy(_ => random.Next()).Take(3))
        {
            yield return new PlannedEvent(product, new InventoryReplenished(new Quantity(random.Next(10, 90))), actor, at, AggregateType.Product, Commands.ReplenishInventory);
        }
    }
}

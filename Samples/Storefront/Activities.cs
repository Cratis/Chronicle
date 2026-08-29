// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.Storefront;

/// <summary>
/// The pieces of work somebody in the storefront does.
/// </summary>
/// <remarks>
/// Each one is a different job on a different kind of thing, which is the point: a store where everybody performs
/// the same workflow gives every scope the same patterns, and there is nothing to discover in that.
/// </remarks>
public enum Activity
{
    /// <summary>Collecting an order from the shelves and boxing it up.</summary>
    Fulfilment = 0,

    /// <summary>Handing packed shipments to a carrier and hearing back that they arrived.</summary>
    Dispatch = 1,

    /// <summary>Answering the support queue.</summary>
    SupportReply = 2,

    /// <summary>Deciding whether something can come back.</summary>
    ReturnDecision = 3,

    /// <summary>Ordering more of a product in.</summary>
    Restocking = 4,

    /// <summary>Moving a product's price.</summary>
    Repricing = 5,

    /// <summary>Looking at an order that was flagged before it ships.</summary>
    FraudReview = 6,

    /// <summary>Topping stock back up overnight.</summary>
    Replenishment = 7
}

/// <summary>
/// The command names the generated history is recorded under.
/// </summary>
/// <remarks>
/// These are what the miner reads as the action, so they are the vocabulary the patterns end up being about.
/// </remarks>
public static class Commands
{
    /// <summary>A customer placing an order.</summary>
    public const string PlaceOrder = nameof(PlaceOrder);

    /// <summary>Collecting an order from the shelves.</summary>
    public const string PickOrder = nameof(PickOrder);

    /// <summary>Boxing picked items up.</summary>
    public const string PackShipment = nameof(PackShipment);

    /// <summary>Handing a shipment to a carrier.</summary>
    public const string DispatchShipment = nameof(DispatchShipment);

    /// <summary>Recording that a shipment arrived.</summary>
    public const string ConfirmDelivery = nameof(ConfirmDelivery);

    /// <summary>A customer opening a support ticket.</summary>
    public const string RaiseTicket = nameof(RaiseTicket);

    /// <summary>Answering a support ticket.</summary>
    public const string AnswerTicket = nameof(AnswerTicket);

    /// <summary>Passing a ticket to somebody else.</summary>
    public const string EscalateTicket = nameof(EscalateTicket);

    /// <summary>A customer asking to send something back.</summary>
    public const string RequestReturn = nameof(RequestReturn);

    /// <summary>Accepting a return.</summary>
    public const string ApproveReturn = nameof(ApproveReturn);

    /// <summary>Turning a return down.</summary>
    public const string RejectReturn = nameof(RejectReturn);

    /// <summary>Ordering more of a product in.</summary>
    public const string RestockProduct = nameof(RestockProduct);

    /// <summary>Moving a product's price.</summary>
    public const string ChangePrice = nameof(ChangePrice);

    /// <summary>Topping stock back up overnight.</summary>
    public const string ReplenishInventory = nameof(ReplenishInventory);

    /// <summary>Pulling an order out for review.</summary>
    public const string HoldOrderForReview = nameof(HoldOrderForReview);

    /// <summary>Letting a held order continue.</summary>
    public const string ReleaseOrder = nameof(ReleaseOrder);

    /// <summary>Stopping an order for good.</summary>
    public const string CancelOrder = nameof(CancelOrder);

    /// <summary>Recording that the sample history was generated.</summary>
    public const string GenerateSampleHistory = nameof(GenerateSampleHistory);
}

/// <summary>
/// The products the catalog holds.
/// </summary>
/// <remarks>
/// A fixed, small catalog on purpose: restocking and repricing act on the same handful of long-lived products
/// across the whole history, so the product read models accumulate something worth querying rather than showing a
/// single event each.
/// </remarks>
public static class Catalog
{
    /// <summary>
    /// The products, with stable identities so a re-run addresses the same ones.
    /// </summary>
    public static readonly ProductId[] Products =
        [.. Enumerable.Range(0, 8).Select(index => Identities.For(index, Identities.ProductMarker))];
}

/// <summary>
/// Builds the stable identities the generated history uses.
/// </summary>
/// <remarks>
/// Derived from a position rather than generated, so a re-run addresses the same event sources. The marker byte
/// keeps the different kinds of thing from colliding with each other.
/// </remarks>
public static class Identities
{
    /// <summary>The marker for an order.</summary>
    public const byte OrderMarker = 0xA1;

    /// <summary>The marker for a shipment.</summary>
    public const byte ShipmentMarker = 0xA2;

    /// <summary>The marker for a return.</summary>
    public const byte ReturnMarker = 0xA3;

    /// <summary>The marker for a support ticket.</summary>
    public const byte TicketMarker = 0xA4;

    /// <summary>The marker for a product.</summary>
    public const byte ProductMarker = 0xA5;

    /// <summary>
    /// Builds an identity from a position and a kind.
    /// </summary>
    /// <param name="index">The position in the run.</param>
    /// <param name="marker">The byte identifying the kind of thing.</param>
    /// <returns>The <see cref="Guid"/> to use as the event source id.</returns>
    public static Guid For(int index, byte marker)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes, index);
        bytes[15] = marker;
        return new Guid(bytes);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Samples.Storefront;

/// <summary>
/// Represents the identity of a customer order.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record OrderId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>
    /// The identity used when not set.
    /// </summary>
    public static readonly OrderId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="OrderId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator OrderId(Guid value) => new(value);
}

/// <summary>
/// Represents the identity of a shipment leaving the warehouse.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ShipmentId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>
    /// The identity used when not set.
    /// </summary>
    public static readonly ShipmentId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="ShipmentId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator ShipmentId(Guid value) => new(value);
}

/// <summary>
/// Represents the identity of a return a customer has asked for.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ReturnId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>
    /// The identity used when not set.
    /// </summary>
    public static readonly ReturnId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="ReturnId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator ReturnId(Guid value) => new(value);
}

/// <summary>
/// Represents the identity of a support ticket.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record TicketId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>
    /// The identity used when not set.
    /// </summary>
    public static readonly TicketId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="TicketId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator TicketId(Guid value) => new(value);
}

/// <summary>
/// Represents the identity of a product in the catalog.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ProductId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>
    /// The identity used when not set.
    /// </summary>
    public static readonly ProductId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="ProductId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator ProductId(Guid value) => new(value);
}

/// <summary>
/// Represents the name of a person - a customer or somebody who works here.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record PersonName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The name used when not set.
    /// </summary>
    public static readonly PersonName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="PersonName"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator PersonName(string value) => new(value);
}

/// <summary>
/// Represents an amount of money.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Money(decimal Value) : ConceptAs<decimal>(Value)
{
    /// <summary>
    /// The amount used when not set.
    /// </summary>
    public static readonly Money NotSet = new(0m);

    /// <summary>
    /// Implicitly convert from <see cref="decimal"/> to <see cref="Money"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator Money(decimal value) => new(value);
}

/// <summary>
/// Represents a count of things - items, packages, units of stock.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Quantity(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// The quantity used when not set.
    /// </summary>
    public static readonly Quantity NotSet = new(0);

    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="Quantity"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator Quantity(int value) => new(value);
}

/// <summary>
/// Represents why something was held, turned down or sent back.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Reason(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The reason used when not set.
    /// </summary>
    public static readonly Reason NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="Reason"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator Reason(string value) => new(value);
}

/// <summary>
/// Represents the carrier a shipment goes out with.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Carrier(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The carrier used when not set.
    /// </summary>
    public static readonly Carrier NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="Carrier"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator Carrier(string value) => new(value);
}

/// <summary>
/// Represents what a support ticket is about.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record TicketTopic(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The topic used when not set.
    /// </summary>
    public static readonly TicketTopic NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="TicketTopic"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator TicketTopic(string value) => new(value);
}

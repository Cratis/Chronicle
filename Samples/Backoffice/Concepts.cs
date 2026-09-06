// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Samples.Backoffice;

/// <summary>
/// Represents the identity of a supplier invoice.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record InvoiceId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>The identity used when not set.</summary>
    public static readonly InvoiceId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="InvoiceId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator InvoiceId(Guid value) => new(value);
}

/// <summary>
/// Represents the identity of a purchase order.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record PurchaseOrderId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>The identity used when not set.</summary>
    public static readonly PurchaseOrderId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="PurchaseOrderId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator PurchaseOrderId(Guid value) => new(value);
}

/// <summary>
/// Represents the identity of a supplier.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record SupplierId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>The identity used when not set.</summary>
    public static readonly SupplierId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="SupplierId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator SupplierId(Guid value) => new(value);
}

/// <summary>
/// Represents the identity of an accounting period in the ledger.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record LedgerId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>The identity used when not set.</summary>
    public static readonly LedgerId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="LedgerId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator LedgerId(Guid value) => new(value);
}

/// <summary>
/// Represents the identity of a request for time off.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record LeaveRequestId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>The identity used when not set.</summary>
    public static readonly LeaveRequestId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="LeaveRequestId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator LeaveRequestId(Guid value) => new(value);
}

/// <summary>
/// Represents the identity of somebody applying for a job.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record CandidateId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>The identity used when not set.</summary>
    public static readonly CandidateId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="CandidateId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator CandidateId(Guid value) => new(value);
}

/// <summary>
/// Represents the identity of somebody's hours for a period.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record TimesheetId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>The identity used when not set.</summary>
    public static readonly TimesheetId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="TimesheetId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator TimesheetId(Guid value) => new(value);
}

/// <summary>
/// Represents the name of a person who works here.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record PersonName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>The name used when not set.</summary>
    public static readonly PersonName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="PersonName"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator PersonName(string value) => new(value);
}

/// <summary>
/// Represents the name of a company we buy from.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record SupplierName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>The name used when not set.</summary>
    public static readonly SupplierName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="SupplierName"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator SupplierName(string value) => new(value);
}

/// <summary>
/// Represents an amount of money.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Money(decimal Value) : ConceptAs<decimal>(Value)
{
    /// <summary>The amount used when not set.</summary>
    public static readonly Money NotSet = new(0m);

    /// <summary>
    /// Implicitly convert from <see cref="decimal"/> to <see cref="Money"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator Money(decimal value) => new(value);
}

/// <summary>
/// Represents a count - of hours, days, or things.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Quantity(int Value) : ConceptAs<int>(Value)
{
    /// <summary>The quantity used when not set.</summary>
    public static readonly Quantity NotSet = new(0);

    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="Quantity"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator Quantity(int value) => new(value);
}

/// <summary>
/// Represents why something was disputed, declined or turned down.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Reason(string Value) : ConceptAs<string>(Value)
{
    /// <summary>The reason used when not set.</summary>
    public static readonly Reason NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="Reason"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator Reason(string value) => new(value);
}

/// <summary>
/// Represents a reference somebody would quote on the phone - an invoice number, a period, a requisition.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Reference(string Value) : ConceptAs<string>(Value)
{
    /// <summary>The reference used when not set.</summary>
    public static readonly Reference NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="Reference"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator Reference(string value) => new(value);
}

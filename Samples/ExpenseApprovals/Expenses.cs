// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Samples.ExpenseApprovals;

/// <summary>
/// Represents the identity of an expense report.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ExpenseReportId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>
    /// The identity used when not set.
    /// </summary>
    public static readonly ExpenseReportId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="ExpenseReportId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator ExpenseReportId(Guid value) => new(value);

    /// <summary>
    /// Creates a new <see cref="ExpenseReportId"/>.
    /// </summary>
    /// <returns>A new <see cref="ExpenseReportId"/>.</returns>
    public static ExpenseReportId New() => new(Guid.NewGuid());
}

/// <summary>
/// Represents the name of a person involved in an expense report.
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
/// Represents a monetary amount on an expense report.
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
/// Represents what an expense was for.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ExpenseCategory(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The category used when not set.
    /// </summary>
    public static readonly ExpenseCategory NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ExpenseCategory"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator ExpenseCategory(string value) => new(value);
}

/// <summary>
/// Represents why an expense report was turned down or sent onwards.
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
/// Emitted when somebody submits an expense report for approval.
/// </summary>
/// <param name="Submitter">The person who submitted it.</param>
/// <param name="Amount">The amount claimed.</param>
/// <param name="Category">What the expense was for.</param>
[EventType]
public record ExpenseReportSubmitted(PersonName Submitter, Money Amount, ExpenseCategory Category);

/// <summary>
/// Emitted when an expense report is approved for reimbursement.
/// </summary>
/// <param name="Approver">The person who approved it.</param>
[EventType]
public record ExpenseReportApproved(PersonName Approver);

/// <summary>
/// Emitted when an expense report is turned down.
/// </summary>
/// <param name="Approver">The person who turned it down.</param>
/// <param name="Reason">Why it was turned down.</param>
[EventType]
public record ExpenseReportRejected(PersonName Approver, Reason Reason);

/// <summary>
/// Emitted when an expense report is passed to somebody with more authority to decide.
/// </summary>
/// <param name="EscalatedTo">The person it was passed to.</param>
/// <param name="Reason">Why it could not be decided at this level.</param>
[EventType]
public record ExpenseReportEscalated(PersonName EscalatedTo, Reason Reason);

/// <summary>
/// Emitted when an approved expense report has been paid out.
/// </summary>
/// <param name="Amount">The amount paid.</param>
[EventType]
public record ExpenseReportReimbursed(Money Amount);

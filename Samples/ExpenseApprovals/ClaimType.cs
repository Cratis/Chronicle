// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Samples.ExpenseApprovals;

/// <summary>
/// Represents what kind of claim an event source is.
/// </summary>
/// <param name="Name">The name of the kind.</param>
/// <remarks>
/// Both kinds go through the same submit-and-decide lifecycle and so share their event types, but they are
/// different kinds of thing and travel different approval routes. Appending them under different event source
/// types is what lets the store tell them apart afterwards - and is what pattern detection reads as the aggregate
/// a habit is about, so "Victor turns down travel claims on a Friday" is discoverable as distinct from whatever
/// he does with ordinary expenses.
/// </remarks>
public record ClaimType(string Name)
{
    /// <summary>
    /// An ordinary expense claim.
    /// </summary>
    public static readonly ClaimType Expense = new("ExpenseReport");

    /// <summary>
    /// A claim for travel, which carries a stricter approval route.
    /// </summary>
    public static readonly ClaimType Travel = new("TravelClaim");

    /// <summary>
    /// Gets the <see cref="Cratis.Chronicle.Events.EventSourceType"/> events of this kind are appended under.
    /// </summary>
    public EventSourceType EventSourceType => new(Name);
}

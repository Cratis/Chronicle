// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a unit of work both enrolls a decision read for an event source and receives an
/// explicit concurrency scope for the same event source.
/// </summary>
/// <param name="scopeLabel">The event source that is both read for a decision and given an explicit scope.</param>
public class DecisionReadConflictsWithConcurrencyScope(EventSourceId scopeLabel)
    : ConflictingConcurrencyScopesForLabel(scopeLabel, Describe(scopeLabel))
{
    static string Describe(EventSourceId scopeLabel) =>
        $"Event source '{scopeLabel}' is both read as a decision read and given an explicit concurrency scope in the same unit of work. " +
        $"The decision read already guards '{scopeLabel}' against events appended after it was read. " +
        $"Remove the explicit concurrency scope for '{scopeLabel}' (for example from EventsWithConcurrencyScopes or an append's concurrency scope), or read it without a decision read.";
}

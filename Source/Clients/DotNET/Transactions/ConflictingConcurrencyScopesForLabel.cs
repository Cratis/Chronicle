// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a unit of work receives conflicting explicit concurrency scopes for one label.
/// </summary>
public class ConflictingConcurrencyScopesForLabel : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictingConcurrencyScopesForLabel"/> class.
    /// </summary>
    /// <param name="scopeLabel">The label that already has an explicit scope.</param>
    /// <param name="enrolledScope">The previously enrolled scope.</param>
    /// <param name="attemptedScope">The conflicting scope that was attempted.</param>
    public ConflictingConcurrencyScopesForLabel(EventSourceId scopeLabel, ConcurrencyScope enrolledScope, ConcurrencyScope attemptedScope)
        : this(scopeLabel, DescribeConflict(scopeLabel, enrolledScope, attemptedScope))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictingConcurrencyScopesForLabel"/> class with a specific message.
    /// </summary>
    /// <param name="scopeLabel">The label whose scopes conflict.</param>
    /// <param name="message">The message that describes the conflict.</param>
    protected ConflictingConcurrencyScopesForLabel(EventSourceId scopeLabel, string message)
        : base(message) => ScopeLabel = scopeLabel;

    /// <summary>
    /// Gets the label, normally the event source id, whose concurrency scopes conflict.
    /// </summary>
    public EventSourceId ScopeLabel { get; }

    static string DescribeConflict(EventSourceId scopeLabel, ConcurrencyScope enrolledScope, ConcurrencyScope attemptedScope) =>
        $"Event source '{scopeLabel}' was given two different concurrency scopes in the same unit of work: first one that " +
        $"{ConcurrencyScopeDescription.Describe(enrolledScope)}, then one that {ConcurrencyScopeDescription.Describe(attemptedScope)}. " +
        $"A unit of work guards each event source with one scope. Capture the scope for '{scopeLabel}' once and use it for every append to it.";
}

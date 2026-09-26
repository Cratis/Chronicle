// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the result of asking an event store to remove an observer.
/// </summary>
/// <param name="Outcome">The <see cref="ObserverRemovalOutcome"/> describing what happened.</param>
/// <param name="BlockingNamespace">
/// The namespace whose observer blocked the removal, when the outcome is a refusal. The guard runs across every
/// namespace, so a refusal that does not name one leaves the operator with nowhere to look.
/// </param>
public record ObserverRemovalResult(ObserverRemovalOutcome Outcome, EventStoreNamespaceName BlockingNamespace)
{
    /// <summary>
    /// Gets a result representing a successful removal.
    /// </summary>
    public static readonly ObserverRemovalResult Removed = new(ObserverRemovalOutcome.Removed, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Gets a result representing an observer that is not registered in the event store.
    /// </summary>
    public static readonly ObserverRemovalResult NotFound = new(ObserverRemovalOutcome.ObserverNotFound, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Creates a result representing an observer that is running in a namespace.
    /// </summary>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the observer is running in.</param>
    /// <returns>An <see cref="ObserverRemovalResult"/>.</returns>
    public static ObserverRemovalResult Active(EventStoreNamespaceName @namespace) => new(ObserverRemovalOutcome.ObserverActive, @namespace);

    /// <summary>
    /// Creates a result representing an observer that still has a subscribed client in a namespace.
    /// </summary>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the observer is subscribed in.</param>
    /// <returns>An <see cref="ObserverRemovalResult"/>.</returns>
    public static ObserverRemovalResult Subscribed(EventStoreNamespaceName @namespace) => new(ObserverRemovalOutcome.ObserverSubscribed, @namespace);
}

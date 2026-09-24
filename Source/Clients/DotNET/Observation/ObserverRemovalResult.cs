// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the result of asking an event store to remove an observer.
/// </summary>
/// <param name="Outcome">The <see cref="ObserverRemovalOutcome"/> describing what happened.</param>
/// <param name="BlockingNamespace">
/// The namespace whose observer blocked the removal, when it was refused; empty otherwise. The guard runs across
/// every namespace in the event store, so a refusal that does not say where leaves nowhere to look.
/// </param>
public record ObserverRemovalResult(ObserverRemovalOutcome Outcome, string BlockingNamespace)
{
    /// <summary>
    /// Gets a value indicating whether the observer was removed.
    /// </summary>
    public bool IsRemoved => Outcome == ObserverRemovalOutcome.Removed;
}

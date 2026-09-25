// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the response from asking an event store to remove an observer.
/// </summary>
[ProtoContract]
public class RemoveObserverResponse
{
    /// <summary>
    /// Gets or sets the <see cref="ObserverRemovalOutcome"/> describing what happened.
    /// </summary>
    [ProtoMember(1)]
    public ObserverRemovalOutcome Outcome { get; set; }

    /// <summary>
    /// Gets or sets the namespace that blocked the removal, when the outcome is a refusal. Empty otherwise.
    /// </summary>
    /// <remarks>
    /// The guard runs across every namespace in the event store, so an operator told only that the observer is still
    /// running has no way of knowing which application to stop. Naming the namespace is the difference between a
    /// refusal that can be acted on and one that cannot.
    /// </remarks>
    [ProtoMember(2)]
    public string BlockingNamespace { get; set; } = string.Empty;
}

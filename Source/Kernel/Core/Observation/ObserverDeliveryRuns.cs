// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>Forms ordered runs for targets with different declared capabilities.</summary>
internal static class ObserverDeliveryRuns
{
    /// <summary>Selects a capable target for the first eligible event.</summary>
    /// <param name="subscription">The committed subscription.</param>
    /// <param name="partition">The partition being delivered.</param>
    /// <param name="first">The first eligible event.</param>
    /// <param name="selector">The configured fan-out selector.</param>
    /// <returns>A capable target, or null when none is connected.</returns>
    internal static ObserverSubscriberTarget? Select(
        ObserverSubscription subscription,
        Key partition,
        AppendedEvent first,
        IObserverSubscriberSelector selector)
    {
        var capable = subscription.Targets.Where(target => CanHandle(target, first.Context.EventType.Id)).ToArray();
        return capable.Length == 0 ? null : selector.Select(subscription with { Targets = capable }, partition);
    }

    /// <summary>Takes the longest ordered prefix the target can handle.</summary>
    /// <param name="target">The selected target.</param>
    /// <param name="events">The remaining eligible events.</param>
    /// <returns>The target-capable prefix.</returns>
    internal static AppendedEvent[] Take(ObserverSubscriberTarget target, IEnumerable<AppendedEvent> events) =>
        events.TakeWhile(@event => CanHandle(target, @event.Context.EventType.Id)).ToArray();

    /// <summary>Applies committed membership and live join fences.</summary>
    /// <param name="subscription">The committed subscription.</param>
    /// <param name="events">Events after ordinary observer filtering.</param>
    /// <returns>The events eligible in their delivery mode.</returns>
    internal static AppendedEvent[] Eligible(ObserverSubscription subscription, IEnumerable<AppendedEvent> events) =>
        events.Where(@event => subscription.TypeSetMembers is { } members &&
            members.TryGetValue(@event.Context.EventType.Id, out var member) &&
            (@event.Context.ObservationState == EventObservationState.Replay || member.JoinFence is not { } fence || @event.Context.SequenceNumber >= fence)).ToArray();

    static bool CanHandle(ObserverSubscriberTarget target, EventTypeId eventType) =>
        target.DeclaredEventTypes?.Any(declared => declared.Id == eventType) == true;
}

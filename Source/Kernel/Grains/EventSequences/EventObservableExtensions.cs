// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Extension methods for <see cref="IObservable{T}"/> for <see cref="EventContext"/>.
/// </summary>
public static class EventObservableExtensions
{
    /// <summary>
    /// Filters an observable to only include events of the specified types.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{EventContext}"/> to filter on.</param>
    /// <param name="types">Collection of <see cref="EventType"/> to filter on.</param>
    /// <returns>A new <see cref="IObservable{EventContext}"/>.</returns>
    public static IObservable<IEnumerable<AppendedEvent>> WhereEventTypesAre(this IObservable<IEnumerable<AppendedEvent>> observable, params EventTypeId[] types) =>
        observable.Where(events => events.Any(@event => types.Contains(@event.Context.EventType.Id)));
}

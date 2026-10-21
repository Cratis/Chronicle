// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles <see cref="EventsWithConcurrencyScopes"/> returned from a reactor handler method by appending its events
/// and concurrency scopes as one operation.
/// </summary>
[Singleton]
public class EventsWithConcurrencyScopesResultHandler : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        value is EventsWithConcurrencyScopes;

    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, IEventStore eventStore, object value) =>
        value is EventsWithConcurrencyScopes;

    /// <inheritdoc/>
    public async Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        var eventsWithConcurrencyScopes = (EventsWithConcurrencyScopes)value;
        var concurrencyScopes = eventsWithConcurrencyScopes.Entries;
        var events = ReactorEffectKeys.Apply(reactorContext, eventStore, eventsWithConcurrencyScopes.Events.ToList());
        var result = concurrencyScopes.GroupBy(_ => _.Key).Any(_ => _.Count() > 1)
            ? await eventStore.EventLog.AppendMany(events, null, null, concurrencyScopes)
            : await eventStore.EventLog.AppendMany(events, concurrencyScopes: concurrencyScopes.ToDictionary(_ => _.Key, _ => _.Value));

        if (!result.IsSuccess)
        {
            return Result.Failed(ReactorSideEffectFailure.FromAppendResult(
                result,
                events.Select(_ => _.EventSourceId),
                reactorContext));
        }

        return Result.Success<ReactorSideEffectFailure>();
    }
}

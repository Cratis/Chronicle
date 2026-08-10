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
        var concurrencyScopes = eventsWithConcurrencyScopes.ConcurrencyScopes.ToDictionary(_ => _.Key, _ => _.Value);
        var result = await eventStore.EventLog.AppendMany(
            eventsWithConcurrencyScopes.Events,
            concurrencyScopes: concurrencyScopes);

        if (!result.IsSuccess)
        {
            return Result.Failed(ReactorSideEffectFailure.FromAppendResult(
                result,
                eventsWithConcurrencyScopes.Events.Select(_ => _.EventSourceId)));
        }

        return Result.Success<ReactorSideEffectFailure>();
    }
}

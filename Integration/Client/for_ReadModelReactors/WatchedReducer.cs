// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[DependencyInjection.IgnoreConvention]
[Reducer("3418-watched-reducer")]
[BatchSize(MaxEvents = 2, MaxConcurrency = 1)]
public class WatchedReducer<T> : IReducerFor<ReducedModel>
{
    public ConcurrentQueue<int> BatchSizes { get; } = new();

    [Observes<Reduction>]
    public ReducedModel? Reduce(EventBatch events, ReducedModel? current)
    {
        BatchSizes.Enqueue(events.Count);
        foreach (var @event in events)
        {
            current = Apply((Reduction)@event.Event, current);
        }
        return current;
    }

    static ReducedModel? Apply(Reduction @event, ReducedModel? current) => @event.Operation switch
    {
        "remove" => null,
        "unchanged" => current,
        "fail" => throw new InvalidOperationException("3418 reducer failure"),
        _ => new(@event.Value)
    };
}

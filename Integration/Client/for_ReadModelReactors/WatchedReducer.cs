// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[DependencyInjection.IgnoreConvention]
[Reducer("34180000-0000-4000-8000-000000000005")]
public class WatchedReducer(WatchedReducer.State state) : IReducerFor<ReducerWatchedReadModel>
{
    readonly Guid _invocation = Guid.NewGuid();

    public ReducerWatchedReadModel On(ReducerWatchedEvent @event, ReducerWatchedReadModel? current, EventContext context)
    {
        state.Invocations.GetOrAdd(_invocation, _ => new()).Enqueue(context.SequenceNumber);
        return new(@event.Value, @event.Marker);
    }

    public ReducerWatchedReadModel? On(ReducerWatchedDelete @event, ReducerWatchedReadModel? current) => null;

    public ReducerWatchedReadModel? On(ReducerWatchedNoChange @event, ReducerWatchedReadModel? current) => current;

    public async Task<ReducerWatchedReadModel> On(ReducerWatchedFailure @event, ReducerWatchedReadModel? current)
    {
        if (Interlocked.Increment(ref state.Failures) == 1)
        {
            state.Failed.TrySetResult();
            throw new InvalidOperationException("Reducer failure for Watch coverage");
        }

        await state.Recover.Task;
        return new(@event.Value, @event.Marker);
    }

    public class State
    {
        public readonly ConcurrentDictionary<Guid, ConcurrentQueue<EventSequenceNumber>> Invocations = new();
        public readonly TaskCompletionSource Failed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Recover = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Failures;
    }
}

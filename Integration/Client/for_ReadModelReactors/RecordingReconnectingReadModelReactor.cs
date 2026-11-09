// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[DependencyInjection.IgnoreConvention]
public class RecordingReconnectingReadModelReactor : IReadModelReactor
{
    readonly ConcurrentDictionary<EventSourceId, int> _counts = new();
    readonly ConcurrentDictionary<EventSourceId, TaskCompletionSource> _added = new();

    public int CountFor(EventSourceId key) => _counts.GetValueOrDefault(key);

    public Task WaitForAdded(EventSourceId key) => SignalFor(key).Task.WaitAsync(TimeSpan.FromSeconds(30));

    public Task Added(WatchedReadModel model, EventContext context)
    {
        _counts.AddOrUpdate(context.EventSourceId, 1, (_, count) => count + 1);
        SignalFor(context.EventSourceId).TrySetResult();
        return Task.CompletedTask;
    }

    TaskCompletionSource SignalFor(EventSourceId key) => _added.GetOrAdd(key, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
}

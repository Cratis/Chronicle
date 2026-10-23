// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Collections.Concurrent;
using Cratis.Chronicle.Storage.EventSequences;
using HandleEventsForObserver = KernelCore::Cratis.Chronicle.Observation.Jobs.HandleEventsForObserver;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

public class ReplayCursorCheckpoint : IIncomingGrainCallFilter
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly AsyncLocal<string?> _bucketStep = new();
    readonly ConcurrentDictionary<string, byte> _entered = new(StringComparer.Ordinal);
    int _armed;

    public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int EnteredBucketSteps => _entered.Count;

    public void Arm() => Volatile.Write(ref _armed, 1);

    public void Release()
    {
        Volatile.Write(ref _armed, 0);
        _release.TrySetResult();
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var previous = _bucketStep.Value;
        _bucketStep.Value = context.TargetContext.GrainInstance is HandleEventsForObserver step && (await step.GetState()).Bucket is not null
            ? context.TargetContext.GrainId.ToString()
            : null;
        try
        {
            await context.Invoke();
        }
        finally
        {
            _bucketStep.Value = previous;
        }
    }

    public object? Observe(string method, object? result) => Volatile.Read(ref _armed) == 1 && method == nameof(IEventSequenceStorage.GetRange) && _bucketStep.Value is { } step && result is Task<IEventCursor> cursor
        ? Hold(step, cursor)
        : result;

    async Task<IEventCursor> Hold(string step, Task<IEventCursor> operation)
    {
        var cursor = await operation;
        _entered.TryAdd(step, 0);
        if (_entered.Count == 8) Reached.TrySetResult();
        await _release.Task;
        return cursor;
    }
}

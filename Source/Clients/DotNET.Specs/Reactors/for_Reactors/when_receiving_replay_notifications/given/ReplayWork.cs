// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_receiving_replay_notifications.given;

public class ReplayWork
{
    readonly ConcurrentQueue<string> _trace = new();
    readonly ConcurrentDictionary<string, TaskCompletionSource> _entered = new();
    readonly ConcurrentDictionary<string, TaskCompletionSource> _gates = new();
    readonly ConcurrentDictionary<string, TaskCompletionSource> _completed = new();
    bool _releasingAll;

    public IReadOnlyList<string> Trace => _trace.ToArray();

    public async Task Run(string name)
    {
        var gate = Signal(_gates, name);
        if (Volatile.Read(ref _releasingAll)) gate.TrySetResult();
        _trace.Enqueue($"{name}:entered");
        Signal(_entered, name).TrySetResult();
        await gate.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _trace.Enqueue($"{name}:completed");
        Signal(_completed, name).TrySetResult();
    }

    public Task Entered(string name) => Signal(_entered, name).Task.WaitAsync(TimeSpan.FromSeconds(10));

    public bool IsCompleted(string name) => Signal(_completed, name).Task.IsCompleted;

    public void Release(string name) => Signal(_gates, name).TrySetResult();

    public void ReleaseAll()
    {
        Volatile.Write(ref _releasingAll, true);
        foreach (var gate in _gates.Values) gate.TrySetResult();
    }

    static TaskCompletionSource Signal(ConcurrentDictionary<string, TaskCompletionSource> signals, string name) =>
        signals.GetOrAdd(name, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
}

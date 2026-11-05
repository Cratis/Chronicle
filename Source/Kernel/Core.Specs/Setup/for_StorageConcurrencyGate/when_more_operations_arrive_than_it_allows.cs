// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.for_StorageConcurrencyGate;

public class when_more_operations_arrive_than_it_allows : Specification
{
    StorageConcurrencyGate _gate;
    int _running;
    int _mostRunning;
    readonly object _lock = new();
    int _completed;
    TaskCompletionSource _release;

    void Establish()
    {
        _gate = new(2);
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        var operations = Enumerable.Range(0, 20).Select(_ => _gate.Run(async () =>
        {
            var now = Interlocked.Increment(ref _running);
            lock (_lock)
            {
                _mostRunning = Math.Max(_mostRunning, now);
            }

            await _release.Task;
            Interlocked.Decrement(ref _running);
            Interlocked.Increment(ref _completed);
        })).ToArray();
        _release.SetResult();
        await Task.WhenAll(operations);
    }

    [Fact] void should_never_run_more_than_allowed() => _mostRunning.ShouldBeLessThanOrEqual(2);
    [Fact] void should_run_them_all() => _completed.ShouldEqual(20);
}

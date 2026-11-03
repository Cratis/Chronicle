// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Hosting.for_ChronicleServerStartupTask.given;

namespace Orleans.Hosting.for_RecoverableStartupWork.when_running;

public class and_more_items_than_the_concurrency_limit : Specification
{
    const int Limit = 2;
    const int Items = 5;

    readonly Lock _lock = new();
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    RecoverableStartupWork _work;
    int _running;
    int _maxRunning;
    int _startedBeforeRelease;
    int _completed;

    void Establish() => _work = new(
        new given.a_virtual_time_provider(),
        new RecordingStartupWorkEscalation(),
        NullLogger<ChronicleServerStartupTask>.Instance,
        maxConcurrentAttempts: Limit);

    async Task Because()
    {
        var runs = Enumerable.Range(0, Items)
            .Select(index => _work.Run(new("ActivateObserver", $"observer-{index}"), _ => Attempt(), CancellationToken.None))
            .ToArray();

        lock (_lock)
        {
            _startedBeforeRelease = _running;
        }

        _release.SetResult();
        await Task.WhenAll(runs).WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    async Task Attempt()
    {
        lock (_lock)
        {
            _running++;
            _maxRunning = Math.Max(_maxRunning, _running);
        }

        await _release.Task;

        lock (_lock)
        {
            _running--;
            _completed++;
        }
    }

    [Fact] void should_hold_back_attempts_beyond_the_limit() => _startedBeforeRelease.ShouldEqual(Limit);
    [Fact] void should_never_run_more_attempts_than_the_limit() => _maxRunning.ShouldEqual(Limit);
    [Fact] void should_complete_every_item() => _completed.ShouldEqual(Items);
}

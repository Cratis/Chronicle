// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Slices.for_ProjectionSlice;

public class when_running_simultaneously : a_projection_slice
{
    Exception?[] _results;
    Barrier _start;

    void Establish()
    {
        _start = new(3);
        _slice.GivenEvents(_source, new Tallied());
    }

    async Task Because()
    {
        var first = RunOnThread();
        var second = RunOnThread();
        _start.SignalAndWait();
        _results = await Task.WhenAll(first, second);
    }

    void Destroy() => _start.Dispose();

    Task<Exception?> RunOnThread() => Task.Factory.StartNew(
        () =>
        {
            _start.SignalAndWait();

            return Catch.Exception(() => _slice.Run().GetAwaiter().GetResult());
        },
        CancellationToken.None,
        TaskCreationOptions.LongRunning,
        TaskScheduler.Default);

    [Fact] void should_complete_one_run() => _results.Count(error => error is null).ShouldEqual(1);
    [Fact] void should_reject_the_other_run() => _results.Count(error => error is SliceAlreadyRun).ShouldEqual(1);
    [Fact] void should_collect_and_process_the_batch_once() => _scenario.Instance!.Count.ShouldEqual(1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverService.when_stopping;

public class with_an_open_bulk_window : Specification
{
    readonly ObserverServiceDrain _drain = new();
    readonly ObserverDetails _observer = new(ObserverKey.NotSet, ObserverType.Projection);
    readonly TaskCompletionSource _flush = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _flushStarted;
    bool _stopCompletedBeforeFlush;

    async Task Establish() => await _drain.Run(_observer, false, () => Task.FromResult(true));

    async Task Because()
    {
        var stop = _drain.Stop(
            (_, _, _) =>
            {
                _flushStarted = true;
                return _flush.Task;
            },
            CancellationToken.None);
        _stopCompletedBeforeFlush = stop.IsCompleted;
        _flush.SetResult();
        await stop.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_flush_the_open_window() => _flushStarted.ShouldBeTrue();
    [Fact] void should_join_the_flush() => _stopCompletedBeforeFlush.ShouldBeFalse();
}

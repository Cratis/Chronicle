// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverService.when_stopping;

public class with_in_flight_catchup : Specification
{
    readonly ObserverServiceDrain _drain = new();
    readonly TaskCompletionSource<bool> _handler = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<bool> _work;
    Task _stop;
    bool _completedBeforeHandler;
    bool _windowEnded;

    void Establish() => _work = _drain.Run(new(ObserverKey.NotSet, ObserverType.Projection), false, () => _handler.Task);

    async Task Because()
    {
        _stop = _drain.Stop(
            (_, _, _) =>
            {
                _windowEnded = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);
        _completedBeforeHandler = _stop.IsCompleted;
        _handler.SetResult(true);
        await _work;
        await _stop.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_join_the_handler() => _completedBeforeHandler.ShouldBeFalse();
    [Fact] void should_end_the_window_after_joining() => _windowEnded.ShouldBeTrue();
}

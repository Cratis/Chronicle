// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverService.when_stopping;

public class and_the_deadline_passes : Specification
{
    readonly ObserverServiceDrain _drain = new();
    readonly CancellationTokenSource _deadline = new();
    readonly TaskCompletionSource<bool> _handler = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<bool> _work;
    bool _completedBeforeDeadline;
    bool _cancelledWindow;

    void Establish() => _work = _drain.Run(new(ObserverKey.NotSet, ObserverType.Projection), false, () => _handler.Task);

    async Task Because()
    {
        var stop = _drain.Stop(
            (_, _, token) =>
            {
                _cancelledWindow = token.IsCancellationRequested;
                return Task.CompletedTask;
            },
            _deadline.Token);
        _completedBeforeDeadline = stop.IsCompleted;
        await _deadline.CancelAsync();
        await stop.WaitAsync(TimeSpan.FromSeconds(5));
        _handler.SetResult(true);
        await _work;
    }

    void Destroy() => _deadline.Dispose();

    [Fact] void should_wait_until_the_shared_deadline() => _completedBeforeDeadline.ShouldBeFalse();
    [Fact] void should_cancel_the_unfinished_window() => _cancelledWindow.ShouldBeTrue();
}

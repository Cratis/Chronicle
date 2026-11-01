// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_RecoverableStartupWork.when_running;

public class and_the_same_item_is_already_in_flight : given.a_recoverable_startup_work
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _calls;
    Task _first;

    async Task Because()
    {
        _first = _work.Run(_item, _ =>
        {
            Interlocked.Increment(ref _calls);
            return _release.Task;
        }, CancellationToken.None);

        await _work.Run(_item, _ =>
        {
            Interlocked.Increment(ref _calls);
            return Task.CompletedTask;
        }, CancellationToken.None);

        _release.SetResult();
        await _first;
    }

    [Fact] void should_run_the_work_only_once() => _calls.ShouldEqual(1);
}

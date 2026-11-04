// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.when_observing;

public class while_the_previous_unsubscribe_is_pending : given.a_gated_reactor_stream
{
    bool _newStreamWaited;

    async Task Because()
    {
        var unsubscribing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishUnsubscribe = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _reactor.Unsubscribe().Returns(_ =>
        {
            unsubscribing.TrySetResult();
            return finishUnsubscribe.Task;
        });
        var oldCancellation = new CancellationTokenSource();
        var old = Start(oldCancellation);
        await _registered.WaitAsync(TimeSpan.FromSeconds(5));
        oldCancellation.Cancel();
        await unsubscribing.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _ownership.Entering = () => waiting.TrySetResult();
        _ = Start(new CancellationTokenSource());
        try
        {
            await waiting.Task.WaitAsync(TimeSpan.FromSeconds(5));
            _newStreamWaited = _registered.CurrentCount == 0;
        }
        finally
        {
            finishUnsubscribe.TrySetResult();
        }

        await old.WaitAsync(TimeSpan.FromSeconds(5));
        await _registered.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_finish_old_unsubscribe_before_new_subscribe() => _newStreamWaited.ShouldBeTrue();
    [Fact] Task should_only_unsubscribe_the_old_stream() => _reactor.Received(1).Unsubscribe();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_an_open_bulk_window;

public class and_the_window_is_closed_during_the_flush : given.a_sink_with_gated_bulk_writes
{
    Exception _error;
    BulkFlushResult _opened;

    async Task Establish()
    {
        await _sink.BeginBulk();
        _opened = await _sink.FlushBulk();
        await Apply(_firstKey, 1, 1UL);
    }

    async Task Because()
    {
        var continuations = new PausedContinuations();
        var previousContext = SynchronizationContext.Current;
        Task<BulkFlushResult> flushing;
        try
        {
            SynchronizationContext.SetSynchronizationContext(continuations);
            flushing = _sink.FlushBulk();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
        await _firstFlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var cancellation = new CancellationTokenSource();
        var closing = _sink.EndBulk(cancellation.Token);

        // Cancellation also wakes the original flush's WaitAsync, even though the driver is still gated.
        // Resume that flush only after EndBulk has cleared the window, not merely started closing it.
        try
        {
            await cancellation.CancelAsync();
            await closing.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            _releaseFirstFlush.SetResult();
            continuations.Resume();
        }
        _error = await Catch.Exception(async () => await flushing.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact] void should_not_confirm_the_closed_window() => _error.ShouldBeOfExactType<BulkWindowLost>();
    [Fact] void should_say_the_window_was_closed() => _error.Message.ShouldContain("closed");
    [Fact] void should_name_the_closed_window() => _error.Message.ShouldContain(_opened.WindowId!.Value.ToString());

    sealed class PausedContinuations : SynchronizationContext
    {
        readonly TaskCompletionSource _resumed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override void Post(SendOrPostCallback d, object? state) => _ = Dispatch(d, state);

        public void Resume() => _resumed.SetResult();

        async Task Dispatch(SendOrPostCallback callback, object? state)
        {
            await _resumed.Task.ConfigureAwait(false);
            callback(state);
        }
    }
}

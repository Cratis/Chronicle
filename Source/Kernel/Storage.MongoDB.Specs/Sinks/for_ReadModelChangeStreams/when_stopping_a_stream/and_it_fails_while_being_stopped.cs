// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_stopping_a_stream;

/// <summary>
/// Whatever comes out of the watch while the stream is being stopped is part of stopping it: it ends the watch
/// quietly, without a log entry and without failing the subscribers.
/// </summary>
public class and_it_fails_while_being_stopped : given.a_container_change_stream
{
    readonly TaskCompletionSource _watching = new(TaskCreationOptions.RunContinuationsAsynchronously);

    Exception _error;
    Channel<bool> _subscriber;

    void Establish() =>
        WatchWith(async cancellationToken =>
        {
            _watching.TrySetResult();

            // Waits for nothing but the stop, then reports it the way a driver in the middle of a network call might:
            // with something that is not a cancellation.
            var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var registration = cancellationToken.Register(() => stopped.TrySetResult());
            await stopped.Task;
            throw new InvalidOperationException("The connection was closed");
        });

    async Task Because()
    {
        _subscriber = Subscriber();
        var stream = CreateStream();
        stream.Add(_subscriber.Writer);
        var watch = stream.Start();
        await _watching.Task.WaitAsync(_deadline);

        stream.Dispose();
        _error = await Catch.Exception(() => watch.WaitAsync(_deadline));
    }

    [Fact] void should_end_the_watch_without_a_fault() => _error.ShouldBeNull();
    [Fact] void should_not_fail_the_subscriber() => _subscriber.Reader.Completion.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_log() => _logger.ReceivedCalls().ShouldBeEmpty();
}

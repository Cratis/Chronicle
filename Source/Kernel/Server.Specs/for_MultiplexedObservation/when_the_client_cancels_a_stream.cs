// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Chronicle.Server.for_MultiplexedObservation;

public class when_the_client_cancels_a_stream : given.a_multiplexed_host
{
    readonly CancellationTokenSource _cancellation = new();
    readonly Subject<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage> _messages = new();
    Exception? _error;

    async Task Because()
    {
        using var subscription = _channel.ObserveReactor(_messages, _cancellation.Token).Subscribe(_ => { }, error => _error = error);
        _messages.OnNext(RegisterReactor("cancelled"));
        await WaitForReactors(1);
        await _cancellation.CancelAsync();
        await _reactors.Calls[0].Detached.WaitAsync(_timeout);
    }

    void Destroy()
    {
        _cancellation.Dispose();
        _messages.Dispose();
    }

    [Fact] void should_end_the_kernel_side_of_the_stream() => _reactors.Calls[0].Detached.IsCompleted.ShouldBeTrue();
    [Fact] void should_end_the_stream_as_cancelled() => (_error is OperationCanceledException).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Multiplexed;
using Cratis.Chronicle.Contracts.Observation.Reactors;

namespace Cratis.Chronicle.Connections.Multiplexed.for_MultiplexedObservationChannel;

public class when_the_kernel_delivers_out_of_order : given.a_channel_over_a_scripted_kernel
{
    readonly Subject<ReactorMessage> _messages = new();
    readonly List<EventsToObserve> _received = [];
    Exception? _error;
    IDisposable _subscription;

    async Task Because()
    {
        _subscription = _channel.ObserveReactor(_messages, CancellationToken.None).Subscribe(_received.Add, error => _error = error);
        _messages.OnNext(Register("reactor"));
        await WaitUntil(() => _calls.Count == 1 && WrittenMessages == 1);
        _calls[0].OnNext(new MultiplexedServerMessage { StreamId = 1, Sequence = 1, Reactor = new() { Partition = "one" } });
        _calls[0].OnNext(new MultiplexedServerMessage { StreamId = 1, Sequence = 3, Reactor = new() { Partition = "three" } });
        await WaitUntil(() => _error is not null);
    }

    void Destroy()
    {
        _subscription.Dispose();
        _messages.Dispose();
    }

    [Fact] void should_deliver_what_arrived_in_order() => _received.Select(_ => _.Partition).ShouldEqual(["one"]);
    [Fact] void should_fail_the_stream() => _error.ShouldBeOfExactType<InvalidOperationException>();
}

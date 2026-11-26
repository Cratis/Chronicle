// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation.Reactors;

namespace Cratis.Chronicle.Connections.Multiplexed.for_MultiplexedObservationChannel;

public class when_the_kernel_call_fails : given.a_channel_over_a_scripted_kernel
{
    readonly Subject<ReactorMessage> _firstMessages = new();
    readonly Subject<ReactorMessage> _secondMessages = new();
    readonly Subject<ReactorMessage> _laterMessages = new();
    Exception? _firstError;
    Exception? _secondError;
    IDisposable _first;
    IDisposable _second;
    IDisposable _later;

    async Task Because()
    {
        _first = _channel.ObserveReactor(_firstMessages, CancellationToken.None).Subscribe(_ => { }, error => _firstError = error);
        _second = _channel.ObserveReactor(_secondMessages, CancellationToken.None).Subscribe(_ => { }, error => _secondError = error);
        _firstMessages.OnNext(Register("first"));
        _secondMessages.OnNext(Register("second"));
        await WaitUntil(() => _calls.Count == 1 && WrittenMessages == 2);
        _calls[0].OnError(new InvalidOperationException("The connection dropped"));
        await WaitUntil(() => _firstError is not null && _secondError is not null);
        _later = _channel.ObserveReactor(_laterMessages, CancellationToken.None).Subscribe(_ => { });
        _laterMessages.OnNext(Register("later"));
        await WaitUntil(() => _calls.Count == 2);
    }

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
        _later.Dispose();
        _firstMessages.Dispose();
        _secondMessages.Dispose();
        _laterMessages.Dispose();
    }

    [Fact] void should_fail_the_first_stream() => _firstError!.Message.ShouldEqual("The connection dropped");
    [Fact] void should_fail_the_second_stream() => _secondError!.Message.ShouldEqual("The connection dropped");
    [Fact] void should_open_a_new_call_for_the_next_stream() => _calls.Count.ShouldEqual(2);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.when_observing;

public class twice_in_the_same_connection_generation : given.a_gated_reactor_stream
{
    Exception _oldError;

    async Task Because()
    {
        var oldCancellation = new CancellationTokenSource();
        var old = Start(oldCancellation);
        await _registered.WaitAsync(TimeSpan.FromSeconds(5));
        _ = Start(new CancellationTokenSource());
        await _registered.WaitAsync(TimeSpan.FromSeconds(5));
        oldCancellation.Cancel();
        _oldError = (await old.WaitAsync(TimeSpan.FromSeconds(5)))!;
    }

    [Fact] void should_finish_the_old_stream_without_error() => _oldError.ShouldBeNull();
    [Fact] void should_not_detach_the_new_mediator_callback() => _mediator.ReceivedCalls().Where(_ => _.GetMethodInfo().Name == "Disconnected").ShouldBeEmpty();
    [Fact] Task should_not_unsubscribe_the_new_stream() => _reactor.DidNotReceive().Unsubscribe();
}

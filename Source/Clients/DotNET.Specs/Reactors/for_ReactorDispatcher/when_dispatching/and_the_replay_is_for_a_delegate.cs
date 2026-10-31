// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching;

public class and_the_replay_is_for_a_delegate : given.a_reactor_dispatcher
{
    int _calls;

    async Task Because() => await _dispatcher.Dispatch(
        _handler,
        (_, _) =>
        {
            _calls++;
            return Task.CompletedTask;
        },
        new() { ReplayState = ReplayState.BeginReplay },
        "ns",
        Publish,
        CancellationToken.None);

    [Fact] void should_not_activate_anything() => _artifactActivator.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_call_the_delegate() => _calls.ShouldEqual(0);
    [Fact] void should_not_publish_a_result() => _published.ShouldBeEmpty();
}

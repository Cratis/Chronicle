// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_reconnecting_a_delegate : given.a_registered_delegate
{
    IReactorHandler _reconnected;
    CancellationToken _originalToken;

    void Establish() => _originalToken = _handler.CancellationToken;

    async Task Because()
    {
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();
        await _reactors.Register();
        _reconnected = _reactors.GetHandlerById("bridge");
    }

    [Fact] void should_disconnect_the_old_handler() => _originalToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_recreate_the_handler() => ReferenceEquals(_reconnected, _handler).ShouldBeFalse();
    [Fact] void should_preserve_both_generations() => _reconnected.EventTypes.Count().ShouldEqual(2);
    [Fact] void should_register_on_both_connections() => _definitions.Count.ShouldEqual(2);
    [Fact] void should_preserve_replay_policy() => _definitions.TrueForAll(_ => !_.IsReplayable).ShouldBeTrue();
}

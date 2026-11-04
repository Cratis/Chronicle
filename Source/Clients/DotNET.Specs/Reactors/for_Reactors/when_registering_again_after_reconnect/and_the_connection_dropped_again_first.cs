// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_the_connection_dropped_again_first : given.a_registered_delegate_that_was_disconnected
{
    IReactorHandler _latest;

    void Establish()
    {
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();
        _latest = _reactors.GetHandlerById("bridge");
    }

    Task Because() => _reactors.RegisterAgainAfterReconnect();

    [Fact] void should_register_on_both_connections() => _definitions.Count.ShouldEqual(2);
    [Fact] void should_keep_the_latest_handler() => ReferenceEquals(_reactors.GetHandlerById("bridge"), _latest).ShouldBeTrue();
    [Fact] void should_not_cancel_the_latest_handler() => _latest.CancellationToken.IsCancellationRequested.ShouldBeFalse();
}

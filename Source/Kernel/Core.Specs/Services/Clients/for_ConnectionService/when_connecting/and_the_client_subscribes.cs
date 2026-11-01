// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

/// <summary>
/// The first heartbeat acknowledges completed registration, not just invocation of the registry.
/// </summary>
public class and_the_client_subscribes : given.a_registration_lifetime
{
    bool _registrationEntered;
    int _heartbeatsWhilePending;

    async Task Because()
    {
        Connect();
        _registrationEntered = await Arrives(_entered.Task);
        _heartbeatsWhilePending = _heartbeats;
        _registration.SetResult();
        _heartbeatArrived = await Arrives(_heartbeat.Task);
    }

    [Fact] void should_invoke_registration() => _registrationEntered.ShouldBeTrue();
    [Fact] void should_not_send_a_heartbeat_while_registration_is_pending() => _heartbeatsWhilePending.ShouldEqual(0);
    [Fact] void should_send_the_first_heartbeat_after_registration() => _heartbeatArrived.ShouldBeTrue();
    [Fact] void should_await_registration_rather_than_just_invoke_it() => _heartbeatBeforeRegistration.ShouldBeFalse();
    [Fact] void should_register_only_once() => Registrations.ShouldEqual(1);
    [Fact] void should_identify_the_connection_it_belongs_to() => _firstKeepAlive.ConnectionId.ShouldEqual("the-client");
}

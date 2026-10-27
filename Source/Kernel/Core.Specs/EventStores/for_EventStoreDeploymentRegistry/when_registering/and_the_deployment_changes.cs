// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventStores.for_EventStoreDeploymentRegistry.when_registering;

public class and_the_deployment_changes : given.a_registry
{
    DeploymentDecision _result;
    bool _registeredBeforeAppend;

    void Establish() => _systemSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), default, default, default, default, default, default, default)
        .Returns(_ =>
        {
            _registeredBeforeAppend = _eventTypes.ReceivedCalls().Any();
            return EventSequences.AppendResult.Success(CorrelationId.NotSet, EventSequenceNumber.First);
        });

    async Task Because() => _result = await _registry.Register(10UL, "latest");

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_register_event_types_before_appending() => _registeredBeforeAppend.ShouldBeTrue();
    [Fact] async Task should_register_system_event_types() => await _eventTypes.Received(1).DiscoverAndRegister(EventStoreName.System);
    [Fact] async Task should_append_exactly_one_deployment_event() => await _systemSequence.Received(1).Append(_name.Value, new EventStoreDeploymentRegistered(_name, 10UL, "latest"), default, default, default, default, default, default, default);
    [Fact] async Task should_return_the_updated_state() => (await _registry.Get()).ShouldEqual(new DeploymentState(10UL, "latest"));
}

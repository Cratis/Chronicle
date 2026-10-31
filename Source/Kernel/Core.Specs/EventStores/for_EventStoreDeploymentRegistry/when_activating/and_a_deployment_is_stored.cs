// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventStores.for_EventStoreDeploymentRegistry.when_activating;

public class and_a_deployment_is_stored : given.a_registry
{
    DeploymentState _result;
    protected override DeploymentState Stored => new(10UL, "stored");

    async Task Because() => _result = await _registry.Get();

    [Fact] void should_return_the_durable_state() => _result.ShouldEqual(Stored);
    [Fact] void should_deserialize_the_last_event() => _serializer.Received(1).Deserialize(_appended);
    [Fact] async Task should_read_only_deployment_events_for_the_store() => await _sequenceStorage.Received(1).TryGetLastInstanceOfAny(_name.Value, Arg.Is<IEnumerable<EventTypeId>>(types => types.SequenceEqual(new[] { typeof(EventStoreDeploymentRegistered).GetEventType().Id })));
}

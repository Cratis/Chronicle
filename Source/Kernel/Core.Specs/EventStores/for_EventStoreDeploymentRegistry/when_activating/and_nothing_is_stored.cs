// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EventStoreDeploymentRegistry.when_activating;

public class and_nothing_is_stored : given.a_registry
{
    DeploymentState _result;

    async Task Because() => _result = await _registry.Get();

    [Fact] void should_return_the_empty_state() => _result.ShouldEqual(DeploymentState.Empty);
    [Fact] void should_not_deserialize_an_absent_event() => _serializer.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_append() => _systemSequence.ReceivedCalls().ShouldBeEmpty();
}

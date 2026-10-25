// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EventStoreDeploymentRegistry.when_registering;

public class and_the_deployment_is_unchanged : given.a_registry
{
    DeploymentDecision _result;
    protected override DeploymentState Stored => new(10UL, "same");

    async Task Because() => _result = await _registry.Register(10UL, "same");

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_not_append() => _systemSequence.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_register_event_types() => _eventTypes.ReceivedCalls().ShouldBeEmpty();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EventStoreDeploymentRegistry.when_registering;

public class and_the_generation_is_stale : given.a_registry
{
    DeploymentDecision _result;
    protected override DeploymentState Stored => new(10UL, "latest");

    async Task Because() => _result = await _registry.Register(9UL, "older");

    [Fact] void should_reject() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_not_append() => _systemSequence.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_keep_the_stored_state() => (await _registry.Get()).ShouldEqual(Stored);
}

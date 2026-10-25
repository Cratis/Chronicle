// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_RegisterDeployment.when_registering;

public class and_the_deployment_is_queried : given.a_registration
{
    EventStoreDeployment _deployment;

    void Establish() => _registry.Get().Returns(new DeploymentState(10UL, "fingerprint"));
    async Task Because() => _deployment = await EventStoreDeployment.GetEventStoreDeployment("the-store", _grainFactory);

    [Fact] void should_expose_the_generation_and_fingerprint() => _deployment.ShouldEqual(new EventStoreDeployment(10UL, "fingerprint"));
}

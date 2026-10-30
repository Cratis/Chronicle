// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions;

public class and_no_generation_is_supplied : given.an_event_store_with_buildable_definitions
{
    async Task Because() => _result = await _eventStore.RegisterDefinitions();

    [Fact]
    void should_send_zero() => _services.EventStores.Received(1).RegisterDeployment(Arg.Is<Contracts.EventStores.RegisterDeploymentRequest>(_ => _.Generation == 0));

    [Fact]
    void should_return_not_set() => _result.Generation.ShouldEqual(DeploymentGeneration.NotSet);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class and_the_cluster_lookup_fails : given.a_registration_with_a_connection
{
    Exception? _exception;

    void Establish() => _management.GetHosts(true).Returns(Task.FromException<Dictionary<SiloAddress, SiloStatus>>(new TimeoutException()));
    async Task Because() => _exception = await Catch.Exception(() => _service.Register(_request));

    [Fact] void should_not_fail_registration() => _exception.ShouldBeNull();
    [Fact] async Task should_still_register_with_the_manager() => await _manager.Received(1).RegisterShared(Arg.Any<string>(), Arg.Is<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(definitions => definitions.Single().Identifier == _projectionDefinition.Identifier), null, null, RegisteredVersion);
}

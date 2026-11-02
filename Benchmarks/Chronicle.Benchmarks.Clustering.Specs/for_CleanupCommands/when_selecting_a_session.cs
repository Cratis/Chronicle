// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.ContainerProfile;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_CleanupCommands;

public class when_selecting_a_session : Specification
{
    DockerCommand _containers = null!;
    DockerCommand _networks = null!;

    void Because()
    {
        _containers = CleanupCommands.Containers("session-under-test");
        _networks = CleanupCommands.Networks("session-under-test");
    }

    [Fact] void should_scope_containers_to_the_session() => _containers.Arguments.ShouldContain("label=cratis.container-profile.session=session-under-test");
    [Fact] void should_scope_networks_to_the_session() => _networks.Arguments.ShouldContain("label=cratis.container-profile.session=session-under-test");
    [Fact] void should_require_harness_owned_containers() => _containers.Arguments.ShouldContain("label=cratis.container-profile=1");
    [Fact] void should_require_harness_owned_networks() => _networks.Arguments.ShouldContain("label=cratis.container-profile=1");
}

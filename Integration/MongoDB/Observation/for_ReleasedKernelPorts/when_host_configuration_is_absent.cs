// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;
using Docker.DotNet.Models;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ReleasedKernelPorts;

public class when_host_configuration_is_absent : Cratis.Specifications.Specification
{
    CreateContainerParameters _parameters;

    void Establish() => _parameters = new();

    void Because() => ReleasedKernelFixture.ReserveKernelPorts(_parameters);

    [Fact] void should_initialize_host_configuration() => _parameters.HostConfig.ShouldNotBeNull();
    [Fact] void should_reserve_the_kernel_listener_ports() => (_parameters.HostConfig?.Sysctls?["net.ipv4.ip_local_reserved_ports"]).ShouldEqual("11111,30000,35000");
}

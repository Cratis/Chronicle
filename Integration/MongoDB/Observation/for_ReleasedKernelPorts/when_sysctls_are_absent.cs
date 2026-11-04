// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;
using Docker.DotNet.Models;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ReleasedKernelPorts;

public class when_sysctls_are_absent : Cratis.Specifications.Specification
{
    CreateContainerParameters _parameters;
    HostConfig _hostConfig;

    void Establish()
    {
        _hostConfig = new() { Memory = 123456 };
        _parameters = new() { HostConfig = _hostConfig };
    }

    void Because() => ReleasedKernelFixture.ReserveKernelPorts(_parameters);

    [Fact] void should_initialize_sysctls() => _parameters.HostConfig.Sysctls.ShouldNotBeNull();
    [Fact] void should_reserve_the_kernel_listener_ports() => (_parameters.HostConfig.Sysctls?["net.ipv4.ip_local_reserved_ports"]).ShouldEqual("11111,30000,35000");
    [Fact] void should_retain_host_configuration() => ReferenceEquals(_parameters.HostConfig, _hostConfig).ShouldBeTrue();
    [Fact] void should_retain_unrelated_host_settings() => _parameters.HostConfig.Memory.ShouldEqual(123456L);
}

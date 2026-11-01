// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;
using Docker.DotNet.Models;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ReleasedKernelPorts;

public class when_sysctls_are_present : Cratis.Specifications.Specification
{
    CreateContainerParameters _parameters;
    IDictionary<string, string> _sysctls;

    void Establish()
    {
        _sysctls = new Dictionary<string, string> { ["net.ipv4.tcp_syncookies"] = "1" };
        _parameters = new() { HostConfig = new() { Sysctls = _sysctls, Memory = 123456 } };
    }

    void Because() => ReleasedKernelFixture.ReserveKernelPorts(_parameters);

    [Fact] void should_reserve_the_kernel_listener_ports() => _parameters.HostConfig.Sysctls["net.ipv4.ip_local_reserved_ports"].ShouldEqual("11111,30000,35000");
    [Fact] void should_retain_unrelated_sysctls() => _parameters.HostConfig.Sysctls["net.ipv4.tcp_syncookies"].ShouldEqual("1");
    [Fact] void should_retain_the_sysctls_dictionary() => ReferenceEquals(_parameters.HostConfig.Sysctls, _sysctls).ShouldBeTrue();
    [Fact] void should_retain_unrelated_host_settings() => _parameters.HostConfig.Memory.ShouldEqual(123456L);
}

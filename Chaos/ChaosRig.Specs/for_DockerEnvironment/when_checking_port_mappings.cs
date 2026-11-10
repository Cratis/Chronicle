// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_DockerEnvironment;

/// <summary>
/// Specifies stable, loopback-only restart mapping validation without using Docker.
/// </summary>
public class when_checking_port_mappings : Specification
{
    [Fact] void should_accept_the_reserved_mapping() => DockerEnvironment.MappingMatches("35000/tcp -> 127.0.0.1:49152\n", 35000, 49152).ShouldBeTrue();
    [Fact] void should_reject_a_changed_port() => DockerEnvironment.MappingMatches("35000/tcp -> 127.0.0.1:49153\n", 35000, 49152).ShouldBeFalse();
    [Fact] void should_reject_a_public_binding() => DockerEnvironment.MappingMatches("35000/tcp -> 0.0.0.0:49152\n", 35000, 49152).ShouldBeFalse();
    [Fact] void should_reject_an_additional_public_binding() => DockerEnvironment.MappingMatches("35000/tcp -> 127.0.0.1:49152\n35000/tcp -> [::]:49152\n", 35000, 49152).ShouldBeFalse();
    [Fact] void should_reject_a_missing_mapping() => DockerEnvironment.MappingMatches(string.Empty, 35000, 49152).ShouldBeFalse();
}

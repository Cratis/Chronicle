// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_TlsCertificateValidationPolicy.when_resolving_the_effective_policy;

public class without_either_input_requesting_validation : given.a_policy_matrix
{
    void Establish() => Configure([(null, null), (true, null), (null, true), (true, true)]);

    void Because() => _results = _inputs.Select(inputs => inputs.Select(input =>
        TlsCertificateValidationPolicy.ShouldSkip(input.Tls, input.ConnectionString)).ToArray()).ToArray();

    [Fact] void should_skip_for_localhost() => _results[0].All(result => result).ShouldBeTrue();
    [Fact] void should_skip_for_ipv4_loopback() => _results[1].All(result => result).ShouldBeTrue();
    [Fact] void should_skip_for_ipv6_loopback() => _results[2].All(result => result).ShouldBeTrue();
    [Fact] void should_skip_for_remote_hosts() => _results[3].All(result => result).ShouldBeTrue();
    [Fact] void should_skip_for_srv_targets() => _results[4].All(result => result).ShouldBeTrue();
    [Fact] void should_skip_for_mixed_hosts() => _results[5].All(result => result).ShouldBeTrue();
}

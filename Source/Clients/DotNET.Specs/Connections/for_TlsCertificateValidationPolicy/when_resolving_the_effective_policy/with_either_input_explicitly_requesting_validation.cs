// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_TlsCertificateValidationPolicy.when_resolving_the_effective_policy;

public class with_either_input_explicitly_requesting_validation : given.a_policy_matrix
{
    void Establish() => Configure([(false, null), (false, false), (false, true), (null, false), (true, false)]);

    void Because() => _results = _inputs.Select(inputs => inputs.Select(input =>
        TlsCertificateValidationPolicy.ShouldSkip(input.Tls, input.ConnectionString)).ToArray()).ToArray();

    [Fact] void should_validate_localhost() => _results[0].Any(result => result).ShouldBeFalse();
    [Fact] void should_validate_ipv4_loopback() => _results[1].Any(result => result).ShouldBeFalse();
    [Fact] void should_validate_ipv6_loopback() => _results[2].Any(result => result).ShouldBeFalse();
    [Fact] void should_validate_remote_hosts() => _results[3].Any(result => result).ShouldBeFalse();
    [Fact] void should_validate_srv_targets() => _results[4].Any(result => result).ShouldBeFalse();
    [Fact] void should_validate_mixed_hosts() => _results[5].Any(result => result).ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_TlsCertificateValidationPolicy.when_resolving_the_effective_policy;

public class with_tls_disabled : given.a_policy_matrix
{
    void Establish() => Configure(
        [(null, null), (null, true), (null, false), (true, null), (true, true), (true, false), (false, null), (false, true), (false, false)],
        disableTls: true);

    void Because() => _results = _inputs.Select(inputs => inputs.Select(input =>
        TlsCertificateValidationPolicy.ShouldSkip(input.Tls, input.ConnectionString)).ToArray()).ToArray();

    [Fact] void should_not_skip_validation_for_localhost() => _results[0].Any(result => result).ShouldBeFalse();
    [Fact] void should_not_skip_validation_for_ipv4_loopback() => _results[1].Any(result => result).ShouldBeFalse();
    [Fact] void should_not_skip_validation_for_ipv6_loopback() => _results[2].Any(result => result).ShouldBeFalse();
    [Fact] void should_not_skip_validation_for_remote_hosts() => _results[3].Any(result => result).ShouldBeFalse();
    [Fact] void should_not_skip_validation_for_srv_targets() => _results[4].Any(result => result).ShouldBeFalse();
    [Fact] void should_not_skip_validation_for_mixed_hosts() => _results[5].Any(result => result).ShouldBeFalse();
}

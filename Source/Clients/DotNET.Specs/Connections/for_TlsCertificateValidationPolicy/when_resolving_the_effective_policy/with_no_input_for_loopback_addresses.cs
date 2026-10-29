// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TlsCertificateValidationPolicy.when_resolving_the_effective_policy;

public class with_no_input_for_loopback_addresses : Specification
{
    bool _result;

    void Because() => _result = TlsCertificateValidationPolicy.ShouldSkip(
        new Tls(),
        new ChronicleConnectionString("chronicle://127.0.0.2:35000,[::1]:35000"));

    [Fact] void should_skip_validation() => _result.ShouldBeTrue();
}

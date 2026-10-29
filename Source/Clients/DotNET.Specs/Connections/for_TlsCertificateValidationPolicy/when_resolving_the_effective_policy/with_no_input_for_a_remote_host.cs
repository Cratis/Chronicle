// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TlsCertificateValidationPolicy.when_resolving_the_effective_policy;

public class with_no_input_for_a_remote_host : Specification
{
    bool _result;

    void Because() => _result = TlsCertificateValidationPolicy.ShouldSkip(
        new Tls(),
        new ChronicleConnectionString("chronicle://chronicle.example:35000"));

    [Fact] void should_validate_the_certificate() => _result.ShouldBeFalse();
}

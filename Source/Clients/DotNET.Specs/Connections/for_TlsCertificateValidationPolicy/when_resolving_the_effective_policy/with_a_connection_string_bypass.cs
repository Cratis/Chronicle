// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TlsCertificateValidationPolicy.when_resolving_the_effective_policy;

public class with_a_connection_string_bypass : Specification
{
    bool _result;

    void Because() => _result = TlsCertificateValidationPolicy.ShouldSkip(
        new Tls { SkipCertificateValidation = false },
        new ChronicleConnectionString("chronicle://localhost:35000?skipTlsValidation=true"));

    [Fact] void should_skip_certificate_validation() => _result.ShouldBeTrue();
}

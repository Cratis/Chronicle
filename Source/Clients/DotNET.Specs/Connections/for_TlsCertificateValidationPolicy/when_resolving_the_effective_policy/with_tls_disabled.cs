// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TlsCertificateValidationPolicy.when_resolving_the_effective_policy;

public class with_tls_disabled : Specification
{
    bool _result;

    void Because() => _result = TlsCertificateValidationPolicy.ShouldSkip(
        new Tls { SkipCertificateValidation = true },
        new ChronicleConnectionString("chronicle://chronicle.example:35000?disableTls=true&skipTlsValidation=true"));

    [Fact] void should_not_apply_a_certificate_validation_override() => _result.ShouldBeFalse();
}

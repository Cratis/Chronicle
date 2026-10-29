// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TlsCertificateValidationPolicy.when_resolving_the_effective_policy;

public class with_an_explicit_connection_string_value_overriding_the_options : Specification
{
    bool _result;

    void Because() => _result = TlsCertificateValidationPolicy.ShouldSkip(
        new Tls { SkipCertificateValidation = false },
        new ChronicleConnectionString("chronicle://chronicle.example:35000?skipTlsValidation=true"));

    [Fact] void should_skip_validation() => _result.ShouldBeTrue();
}

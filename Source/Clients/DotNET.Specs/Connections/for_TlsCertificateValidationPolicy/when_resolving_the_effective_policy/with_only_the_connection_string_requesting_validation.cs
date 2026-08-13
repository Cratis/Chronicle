// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TlsCertificateValidationPolicy.when_resolving_the_effective_policy;

/// <summary>
/// One setting has to be enough. The untouched options value still defaults to skipping, so combining
/// the two with OR would swallow this request and go on accepting any certificate.
/// </summary>
public class with_only_the_connection_string_requesting_validation : Specification
{
    bool _result;

    void Because() => _result = TlsCertificateValidationPolicy.ShouldSkip(
        new Tls(),
        new ChronicleConnectionString("chronicle://localhost:35000?skipTlsValidation=false"));

    [Fact] void should_validate_the_certificate() => _result.ShouldBeFalse();
}

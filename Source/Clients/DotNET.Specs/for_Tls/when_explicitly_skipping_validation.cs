// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_Tls;

public class when_explicitly_skipping_validation : Specification
{
    Tls _tls;

    void Establish() => _tls = new();

    void Because() => _tls.SkipCertificateValidation = true;

    [Fact] void should_skip_certificate_validation() => _tls.SkipCertificateValidation.ShouldBeTrue();
    [Fact] void should_mark_validation_as_explicitly_configured() => _tls.HasExplicitSkipCertificateValidation.ShouldBeTrue();
}

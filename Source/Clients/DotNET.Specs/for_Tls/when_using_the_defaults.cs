// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_Tls;

/// <summary>
/// Remote connections validate server certificates unless validation is explicitly skipped.
/// </summary>
public class when_using_the_defaults : Specification
{
    Tls _tls;

    void Because() => _tls = new Tls();

    [Fact] void should_require_certificate_validation() => _tls.SkipCertificateValidation.ShouldBeFalse();
}

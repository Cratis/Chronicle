// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_Tls;

/// <summary>
/// The Chronicle server generates an in-memory self-signed certificate on every start when none is
/// configured, so a development client has nothing it could validate against. Skipping validation by
/// default is what lets a development server and client connect without any certificate setup.
/// </summary>
public class when_using_the_defaults : Specification
{
    Tls _tls;

    void Because() => _tls = new Tls();

    [Fact] void should_skip_certificate_validation() => _tls.SkipCertificateValidation.ShouldBeTrue();
}

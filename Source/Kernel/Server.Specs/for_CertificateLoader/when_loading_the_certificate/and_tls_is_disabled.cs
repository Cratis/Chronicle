// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Server.for_CertificateLoader.when_loading_the_certificate;

public class and_tls_is_disabled : given.a_certificate_file
{
    X509Certificate2 _result;

    void Establish() => WritePkcs12(password: null);

    void Because() => _result = CertificateLoader.LoadCertificate(OptionsWithTls(password: null, enabled: false));

    [Fact] void should_not_load_the_configured_certificate() => _result.ShouldBeNull();
}

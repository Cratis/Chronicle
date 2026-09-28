// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Server.for_CertificateLoader.when_loading_the_certificate;

public class and_only_the_dedicated_health_listener_uses_tls : given.a_certificate_file
{
    X509Certificate2 _result;

    void Establish() => WritePkcs12(password: null);

    void Because() => _result = CertificateLoader.LoadHealthCertificate(OptionsWithTls(password: null, enabled: false));

    [Fact] void should_load_the_certificate_independently_of_the_main_listener() => _result.ShouldNotBeNull();
    [Fact] void should_load_the_same_certificate() => _result.Thumbprint.ShouldEqual(_sourceCertificate.Thumbprint);

    void Destroy() => _result?.Dispose();
}

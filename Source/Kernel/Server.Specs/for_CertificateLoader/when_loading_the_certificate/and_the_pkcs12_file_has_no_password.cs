// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Server.for_CertificateLoader.when_loading_the_certificate;

public class and_the_pkcs12_file_has_no_password : given.a_certificate_file
{
    X509Certificate2 _result;
    Exception _exception;

    void Establish() => WritePkcs12(password: null);

    void Because() => _exception = Catch.Exception(() => _result = CertificateLoader.LoadCertificate(OptionsWithTls(password: null)));

    void Destroy() => _result?.Dispose();

    [Fact] void should_load_the_certificate() => _exception.ShouldBeNull();
    [Fact] void should_load_the_configured_certificate() => _result.Thumbprint.ShouldEqual(_sourceCertificate.Thumbprint);
    [Fact] void should_load_the_private_key_the_tls_listener_needs() => _result.HasPrivateKey.ShouldBeTrue();
}

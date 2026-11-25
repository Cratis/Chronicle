// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_loading_certificate_authorities;

public class and_the_file_has_a_der_pkcs7_bundle : given.a_private_certificate_authority
{
    X509Certificate2Collection _result;

    void Establish() => File.WriteAllBytes(_certificateAuthorityPath, _certificateAuthorities.Export(X509ContentType.Pkcs7)!);

    void Because() => _result = CertificateLoader.LoadCertificateAuthorities(_certificateAuthorityPath);

    [Fact] void should_load_one_certificate_authority() => _result.Count.ShouldEqual(1);
    [Fact] void should_load_the_certificate_authority() => _result[0].Thumbprint.ShouldEqual(_certificateAuthority.Thumbprint);

    void Destroy()
    {
        foreach (var certificate in _result)
        {
            certificate.Dispose();
        }
    }
}

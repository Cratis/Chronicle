// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_loading_certificate_authorities;

public class and_the_file_has_a_pkcs7_bundle : given.a_private_certificate_authority
{
    X509Certificate2 _secondCertificateAuthority;
    string _bundlePath;
    X509Certificate2Collection _result;

    void Establish()
    {
        _secondCertificateAuthority = CreateCertificateAuthority("CN=chronicle-specs-second-ca");
        _bundlePath = Path.Combine(Path.GetTempPath(), $"chronicle-client-ca-bundle-{Guid.NewGuid():N}.p7b");
        var bundle = new X509Certificate2Collection { _certificateAuthority, _secondCertificateAuthority }.Export(X509ContentType.Pkcs7);
        File.WriteAllText(_bundlePath, new string(PemEncoding.Write("PKCS7", bundle)));
    }

    void Because() => _result = CertificateLoader.LoadCertificateAuthorities(_bundlePath);

    void Destroy()
    {
        foreach (var certificate in _result)
        {
            certificate.Dispose();
        }

        _secondCertificateAuthority.Dispose();
        File.Delete(_bundlePath);
    }

    [Fact] void should_load_both_certificate_authorities() => _result.Count.ShouldEqual(2);
    [Fact] void should_load_the_first_certificate_authority() => _result.Any(certificate => certificate.Thumbprint == _certificateAuthority.Thumbprint).ShouldBeTrue();
    [Fact] void should_load_the_second_certificate_authority() => _result.Any(certificate => certificate.Thumbprint == _secondCertificateAuthority.Thumbprint).ShouldBeTrue();
}

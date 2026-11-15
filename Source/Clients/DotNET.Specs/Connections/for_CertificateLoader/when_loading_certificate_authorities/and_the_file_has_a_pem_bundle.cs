// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_loading_certificate_authorities;

public class and_the_file_has_a_pem_bundle : given.a_private_certificate_authority
{
    X509Certificate2 _secondCertificateAuthority;
    string _bundlePath;
    X509Certificate2Collection _result;

    void Establish()
    {
        _secondCertificateAuthority = CreateCertificateAuthority("CN=chronicle-specs-second-ca");
        _bundlePath = Path.Combine(Path.GetTempPath(), $"chronicle-client-ca-bundle-{Guid.NewGuid():N}.pem");
        File.WriteAllText(_bundlePath, $"{_certificateAuthority.ExportCertificatePem()}\n{_secondCertificateAuthority.ExportCertificatePem()}\n");
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
    [Fact] void should_load_the_first_certificate_authority() => _result[0].Thumbprint.ShouldEqual(_certificateAuthority.Thumbprint);
    [Fact] void should_load_the_second_certificate_authority() => _result[1].Thumbprint.ShouldEqual(_secondCertificateAuthority.Thumbprint);
}

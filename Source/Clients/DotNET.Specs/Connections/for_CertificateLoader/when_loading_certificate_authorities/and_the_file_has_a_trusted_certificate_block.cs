// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_loading_certificate_authorities;

public class and_the_file_has_a_trusted_certificate_block : given.a_private_certificate_authority
{
    string _trustedCertificatePath;
    Exception _failure;
    X509Certificate2Collection _result;

    void Establish()
    {
        _trustedCertificatePath = Path.Combine(Path.GetTempPath(), $"chronicle-client-ca-trusted-{Guid.NewGuid():N}.pem");
        File.WriteAllText(_trustedCertificatePath, $"{_certificateAuthority.ExportCertificatePem()}\n{new string(PemEncoding.Write("TRUSTED CERTIFICATE", _certificateAuthority.RawData))}\n");
    }

    void Because() => _failure = Catch.Exception(() => _result = CertificateLoader.LoadCertificateAuthorities(_trustedCertificatePath));

    void Destroy() => File.Delete(_trustedCertificatePath);

    [Fact] void should_fail_with_an_unsupported_certificate_authority_format() => _failure.ShouldBeOfExactType<UnsupportedCertificateAuthorityFormat>();
    [Fact] void should_not_load_any_certificate_authorities() => _result.ShouldBeNull();
}

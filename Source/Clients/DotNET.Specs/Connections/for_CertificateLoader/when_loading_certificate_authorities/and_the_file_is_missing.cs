// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_loading_certificate_authorities;

public class and_the_file_is_missing : Specification
{
    string _path;
    Exception _failure;
    X509Certificate2Collection _result;

    void Establish() => _path = Path.Combine(Path.GetTempPath(), $"chronicle-client-ca-missing-{Guid.NewGuid():N}.pem");

    void Because() => _failure = Catch.Exception(() => _result = CertificateLoader.LoadCertificateAuthorities(_path));

    [Fact] void should_fail_with_a_missing_certificate_authority() => _failure.ShouldBeOfExactType<CertificateAuthorityDoesNotExist>();
    [Fact] void should_not_load_any_certificate_authorities() => _result.ShouldBeNull();
}

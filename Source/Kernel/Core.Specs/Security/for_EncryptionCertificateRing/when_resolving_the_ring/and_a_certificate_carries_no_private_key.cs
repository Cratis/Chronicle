// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EncryptionCertificateRing.when_resolving_the_ring;

public class and_a_certificate_carries_no_private_key : given.certificate_files
{
    string _publicOnlyPath;
    Exception _exception;

    void Establish() => _publicOnlyPath = WritePkcs12WithoutPrivateKey(_previousCertificate);

    void Because() => _exception = Catch.Exception(() =>
        _ = EncryptionCertificateRing.From(new Configuration.ChronicleOptions
        {
            EncryptionCertificate = Configuration(_activeCertificatePath, _publicOnlyPath)
        }).All.ToArray());

    [Fact] void should_fail_closed_because_it_could_never_decrypt() => _exception.ShouldBeOfExactType<EncryptionCertificateWithoutPrivateKey>();
    [Fact] void should_name_the_certificate_that_carries_no_key() => _exception.Message.ShouldContain(_previousCertificate.Thumbprint);
}

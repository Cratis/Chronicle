// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EncryptionCertificateRing.when_resolving_the_ring;

public class and_the_same_certificate_is_active_and_previous : given.certificate_files
{
    string _copyOfTheActiveCertificate;
    Exception _exception;

    void Establish() => _copyOfTheActiveCertificate = WritePkcs12(_activeCertificate, Password);

    void Because() => _exception = Catch.Exception(() =>
        _ = EncryptionCertificateRing.From(new Configuration.ChronicleOptions
        {
            EncryptionCertificate = Configuration(_activeCertificatePath, _copyOfTheActiveCertificate)
        }).All.ToArray());

    [Fact] void should_reject_an_overlap_that_is_not_one() => _exception.ShouldBeOfExactType<DuplicateEncryptionCertificateInRing>();
    [Fact] void should_name_the_key_id_that_appears_twice() => _exception.Message.ShouldContain(_activeCertificate.Thumbprint);
}

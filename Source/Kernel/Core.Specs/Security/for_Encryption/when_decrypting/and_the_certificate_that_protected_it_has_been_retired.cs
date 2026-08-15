// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_Encryption.when_decrypting;

public class and_the_certificate_that_protected_it_has_been_retired : given.two_certificates
{
    string _protectedByTheRetiredCertificate;
    Exception _exception;

    void Establish() => _protectedByTheRetiredCertificate = EncryptionWith(_firstCertificate).Encrypt(TheSecret);

    void Because() => _exception = Catch.Exception(() => EncryptionWith(_secondCertificate).Decrypt(_protectedByTheRetiredCertificate));

    [Fact] void should_report_the_value_as_unreadable() => _exception.ShouldBeOfExactType<EncryptionCertificateNotInRing>();
    [Fact] void should_name_the_key_id_the_value_needs() => _exception.Message.ShouldContain(_firstCertificate.Thumbprint);
    [Fact] void should_name_the_key_ids_the_ring_holds() => _exception.Message.ShouldContain(_secondCertificate.Thumbprint);
    [Fact] void should_not_expose_the_protected_value() => _exception.Message.Contains(_protectedByTheRetiredCertificate.Split(':')[2], StringComparison.Ordinal).ShouldBeFalse();
}

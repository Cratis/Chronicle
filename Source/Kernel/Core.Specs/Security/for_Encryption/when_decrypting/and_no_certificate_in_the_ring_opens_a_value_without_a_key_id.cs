// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_Encryption.when_decrypting;

public class and_no_certificate_in_the_ring_opens_a_value_without_a_key_id : given.two_certificates
{
    string _writtenBeforeValuesCarriedKeyIds;
    Exception _exception;

    void Establish() => _writtenBeforeValuesCarriedKeyIds = EncryptWithoutKeyId(_firstCertificate, TheSecret);

    void Because() => _exception = Catch.Exception(() => EncryptionWith(_secondCertificate).Decrypt(_writtenBeforeValuesCarriedKeyIds));

    [Fact] void should_report_the_value_as_unreadable() => _exception.ShouldBeOfExactType<ValueNotDecryptableWithAnyCertificate>();
    [Fact] void should_name_the_key_ids_the_ring_holds() => _exception.Message.ShouldContain(_secondCertificate.Thumbprint);
    [Fact] void should_not_expose_the_protected_value() => _exception.Message.Contains(_writtenBeforeValuesCarriedKeyIds, StringComparison.Ordinal).ShouldBeFalse();
}

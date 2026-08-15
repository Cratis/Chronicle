// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_Encryption.when_decrypting;

public class and_the_value_carries_no_key_id : given.two_certificates
{
    string _writtenBeforeValuesCarriedKeyIds;
    string _result;

    void Establish() => _writtenBeforeValuesCarriedKeyIds = EncryptWithoutKeyId(_firstCertificate, TheSecret);

    void Because() => _result = EncryptionWith(_secondCertificate, _firstCertificate).Decrypt(_writtenBeforeValuesCarriedKeyIds);

    [Fact] void should_read_it_by_trying_the_ring() => _result.ShouldEqual(TheSecret);
}

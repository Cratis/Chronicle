// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_Encryption.when_encrypting;

public class and_the_value_is_empty : given.two_certificates
{
    string _result;

    void Because() => _result = EncryptionWith(_firstCertificate).Encrypt(string.Empty);

    [Fact] void should_leave_it_alone_rather_than_label_nothing() => _result.ShouldEqual(string.Empty);
}

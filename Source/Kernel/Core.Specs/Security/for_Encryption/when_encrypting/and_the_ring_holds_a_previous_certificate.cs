// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_Encryption.when_encrypting;

public class and_the_ring_holds_a_previous_certificate : given.two_certificates
{
    string _result;

    void Because() => _result = EncryptionWith(_secondCertificate, _firstCertificate).Encrypt(TheSecret);

    [Fact] void should_protect_it_with_the_active_certificate() => _result.StartsWith($"{Encryption.KeyIdPrefix}:{_secondCertificate.Thumbprint}:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_protect_it_with_the_previous_certificate() => _result.Contains(_firstCertificate.Thumbprint, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_read_back_through_the_active_certificate_alone() => EncryptionWith(_secondCertificate).Decrypt(_result).ShouldEqual(TheSecret);
}

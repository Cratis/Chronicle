// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_Encryption.when_decrypting;

public class and_the_value_was_protected_by_the_previous_certificate : given.two_certificates
{
    string _protectedBeforeTheRotation;
    string _protectedAfterTheRotation;
    string _result;
    string _resultForTheNewValue;

    void Establish()
    {
        _protectedBeforeTheRotation = EncryptionWith(_firstCertificate).Encrypt(TheSecret);
        _protectedAfterTheRotation = EncryptionWith(_secondCertificate, _firstCertificate).Encrypt(TheSecret);
    }

    void Because()
    {
        var afterTheRotation = EncryptionWith(_secondCertificate, _firstCertificate);
        _result = afterTheRotation.Decrypt(_protectedBeforeTheRotation);
        _resultForTheNewValue = afterTheRotation.Decrypt(_protectedAfterTheRotation);
    }

    [Fact] void should_still_read_what_the_previous_certificate_protected() => _result.ShouldEqual(TheSecret);
    [Fact] void should_read_what_the_active_certificate_protects() => _resultForTheNewValue.ShouldEqual(TheSecret);
    [Fact] void should_have_protected_the_new_value_with_the_active_certificate() => _protectedAfterTheRotation.StartsWith($"{Encryption.KeyIdPrefix}:{_secondCertificate.Thumbprint}:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_have_protected_the_old_value_with_the_previous_certificate() => _protectedBeforeTheRotation.StartsWith($"{Encryption.KeyIdPrefix}:{_firstCertificate.Thumbprint}:", StringComparison.Ordinal).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.for_Encryption;

public class when_verifying_a_signed_identity : given.a_signed_value
{
    string _fingerprint;
    Exception _atAnotherAddress;
    Exception _withTamperedCiphertext;
    Exception _withTamperedSignature;

    void Because()
    {
        _fingerprint = EnvelopeSigning.Verify(EncryptionEnvelope.Read(_encrypted), _address);
        _atAnotherAddress = Catch.Exception(() => EnvelopeSigning.Verify(EncryptionEnvelope.Read(_encrypted), _address with { Identifier = "another subject" }));
        _withTamperedCiphertext = Catch.Exception(() => EnvelopeSigning.Verify(EncryptionEnvelope.Read(Tamper(_encrypted, _encrypted.Length - 65)), _address));
        _withTamperedSignature = Catch.Exception(() => EnvelopeSigning.Verify(EncryptionEnvelope.Read(Tamper(_encrypted, _encrypted.Length - 1)), _address));
    }

    static byte[] Tamper(byte[] bytes, int index)
    {
        var copy = (byte[])bytes.Clone();
        copy[index] ^= 1;
        return copy;
    }

    [Fact] void should_yield_the_fingerprint_of_the_managed_key() => _fingerprint.ShouldEqual(_key.Fingerprint);
    [Fact] void should_fail_at_another_address() => _atAnotherAddress.ShouldBeOfExactType<EncryptionEnvelopeBindingFailed>();
    [Fact] void should_fail_when_the_ciphertext_is_tampered_with() => _withTamperedCiphertext.ShouldBeOfExactType<EncryptionEnvelopeBindingFailed>();
    [Fact] void should_fail_when_the_signature_is_tampered_with() => _withTamperedSignature.ShouldBeOfExactType<EncryptionEnvelopeBindingFailed>();
}

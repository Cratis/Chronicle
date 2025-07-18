// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_Encryption;

public class when_encrypting_with_a_generated_key : Specification
{
    Encryption _encryption;
    EncryptionKey _key;
    byte[] _before;
    byte[] _encrypted;
    byte[] _decrypted;

    void Establish()
    {
        _encryption = new();
        _key = _encryption.GenerateKey();

        _before = Encoding.UTF8.GetBytes("Hello world");
    }

    void Because()
    {
        _encrypted = _encryption.Encrypt(_before, _key);
        _decrypted = _encryption.Decrypt(_encrypted, _key);
    }

    [Fact] void should_encrypt_to_something_that_is_not_equal_to_original() => _encrypted.ShouldNotEqual(_before);
    [Fact] void should_decrypt_to_the_same_as_original() => _decrypted.ShouldEqual(_before);
}

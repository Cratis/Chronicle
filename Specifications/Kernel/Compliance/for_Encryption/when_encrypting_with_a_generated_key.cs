// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Aksio.Cratis.Kernel.Storage.Compliance;

namespace Aksio.Cratis.Kernel.Compliance.for_Encryption;

public class when_encrypting_with_a_generated_key : Specification
{
    Encryption encryption;
    EncryptionKey key;
    byte[] before;
    byte[] encrypted;
    byte[] decrypted;

    void Establish()
    {
        encryption = new();
        key = encryption.GenerateKey();

        before = Encoding.UTF8.GetBytes("Hello world");
    }

    void Because()
    {
        encrypted = encryption.Encrypt(before, key);
        decrypted = encryption.Decrypt(encrypted, key);
    }

    [Fact] void should_encrypt_to_something_that_is_not_equal_to_original() => encrypted.ShouldNotEqual(before);
    [Fact] void should_decrypt_to_the_same_as_original() => decrypted.ShouldEqual(before);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Compliance.for_Encryption;

/// <summary>
/// Releasing has to tell a value that was encrypted from one that never was — a value resolved in memory at the
/// query edge, or one written before its property was marked [PII]. Without that, releasing the latter either
/// blanks it (silent data loss) or throws (failing the whole query over one property).
/// </summary>
public class when_checking_if_a_value_is_encrypted : given.a_key
{
    [Fact] void should_recognize_a_value_it_encrypted() =>
        _encryption.IsEncrypted(_encryption.Encrypt(Encoding.UTF8.GetBytes("sensitive"), _key)).ShouldBeTrue();

    [Fact] void should_recognize_an_encrypted_empty_value() =>
        _encryption.IsEncrypted(_encryption.Encrypt([], _key)).ShouldBeTrue();

    [Fact] void should_not_recognize_plaintext() =>
        _encryption.IsEncrypted(Encoding.UTF8.GetBytes("Jane Doe")).ShouldBeFalse();

    [Fact] void should_not_recognize_plaintext_that_is_short_enough_to_decode_as_base64() =>
        _encryption.IsEncrypted(Encoding.UTF8.GetBytes("Jane")).ShouldBeFalse();

    [Fact] void should_not_recognize_no_value_at_all() =>
        _encryption.IsEncrypted([]).ShouldBeFalse();
}

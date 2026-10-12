// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyText;

public class when_hashing_raw_code_units_in_chunks : Specification
{
    byte[] _digest;
    string _text;

    void Establish() => _text = new string('t', 255) + "\uD800\uDC00\uD800x" + new string('t', 9000);
    void Because() => _digest = SequencingKeyText.Utf16Digest(_text);

    [Fact] void should_match_the_raw_little_endian_hash() =>
        Convert.ToHexString(_digest).ShouldEqual(Convert.ToHexString(SHA256.HashData(SequencingKeyText.Utf16LittleEndian(_text))));
}

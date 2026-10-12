// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing_an_unresolved_key;

public class with_multibyte_text_at_the_byte_limit : Specification
{
    CanonicalSequencingKey _text;
    CanonicalSequencingKey _digest;

    void Because()
    {
        _text = CanonicalSequencingKey.From(SequencingKey.Unresolved(new string('\u00e9', 4093)));
        _digest = CanonicalSequencingKey.From(SequencingKey.Unresolved(new string('\u00e9', 4093) + "a"));
    }

    [Fact] void should_accept_8186_utf8_bytes_as_text() => _text.Span.Length.ShouldEqual(8192);
    [Fact] void should_select_digest_above_the_byte_limit() => _digest.Span[1].ShouldEqual((byte)UnresolvedKeyForm.Utf16Digest);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing_an_unresolved_key;

public class with_a_large_malformed_event_type : Specification
{
    string _text;
    CanonicalSequencingKey _key;
    long _allocated;

    void Establish() => _text = "\uD800" + new string('t', 1_000_000);
    void Because()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        _key = CanonicalSequencingKey.From(SequencingKey.Unresolved(_text));
        _allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact] void should_use_the_bounded_digest_form() => _key.Span.Length.ShouldEqual(34);
    [Fact] void should_not_allocate_an_input_sized_encoding_buffer() => _allocated.ShouldBeLessThan(65_536L);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_parsing_stored_bytes;

public class with_a_short_event_source_id_digest : Specification
{
    byte[] _bytes;
    Exception? _error;

    void Establish()
    {
        _bytes = new byte[32];
        _bytes[0] = (byte)SequencingKeyKind.EventSourceIdDigest;
    }

    void Because() => _error = Catch.Exception(() => CanonicalSequencingKey.FromStored(_bytes));

    [Fact] void should_reject_the_short_digest() => _error.ShouldBeOfExactType<InvalidCanonicalSequencingKey>();
}

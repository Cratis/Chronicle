// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_parsing_stored_bytes;

public class a_round_tripped_value_key : Specification
{
    CanonicalSequencingKey _original;
    CanonicalSequencingKey _restored;

    void Establish() => _original = CanonicalSequencingKey.From(given.golden_keys.Key(5));
    void Because() => _restored = CanonicalSequencingKey.FromStored(_original.Span);

    [Fact] void should_restore_the_canonical_value() => _restored.ShouldEqual(_original);
    [Fact] void should_restore_the_description() => _restored.Describe().ShouldEqual(_original.Describe());
    [Fact] void should_preserve_the_value_hash_code() => _restored.GetHashCode().ShouldEqual(_original.GetHashCode());
}

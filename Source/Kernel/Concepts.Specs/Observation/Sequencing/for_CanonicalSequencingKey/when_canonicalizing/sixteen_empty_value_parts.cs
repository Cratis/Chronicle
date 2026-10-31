// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing;

public class sixteen_empty_value_parts : Specification
{
    CanonicalSequencingKey _key;

    void Because() => _key = CanonicalSequencingKey.From(new ValueSequencingKey(Enumerable.Repeat("", 16).ToImmutableArray()));

    [Fact] void should_keep_all_sixteen_parts() => _key.Describe().Parts.Length.ShouldEqual(16);
    [Fact] void should_round_trip_the_empty_parts() => CanonicalSequencingKey.FromStored(_key.Span).ShouldEqual(_key);
}

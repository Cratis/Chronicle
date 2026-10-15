// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing_an_unresolved_key;

public class with_a_null_event_type : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => SequencingKey.Unresolved(null!));

    [Fact] void should_reject_the_missing_event_type() => _error.ShouldBeOfExactType<InvalidSequencingKey>();
}

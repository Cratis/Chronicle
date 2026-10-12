// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKey;

public class when_copying_into_a_foreign_type : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => _ = new given.foreign_key(SequencingKey.ForEventSourceId("abc")));

    [Fact] void should_keep_the_hierarchy_closed() => _error.ShouldBeOfExactType<InvalidSequencingKey>();
}

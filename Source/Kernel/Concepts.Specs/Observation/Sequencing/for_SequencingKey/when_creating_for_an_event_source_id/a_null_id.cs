// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKey.when_creating_for_an_event_source_id;

public class a_null_id : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => SequencingKey.ForEventSourceId(null!));

    [Fact] void should_reject_the_missing_id() => _error.ShouldBeOfExactType<InvalidSequencingKey>();
}

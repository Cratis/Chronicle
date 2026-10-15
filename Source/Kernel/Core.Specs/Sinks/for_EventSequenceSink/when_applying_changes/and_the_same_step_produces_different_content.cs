// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_the_same_step_produces_different_content : given.a_sink
{
    Exception _error;

    async Task Establish() => await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);
    async Task Because() => _error = await Catch.Exception(() => _sink.ApplyChanges("customer-1", Fold(new(), 99, 1, 5), 5));

    [Fact] void should_fail_explicitly_instead_of_publishing_a_second_instance() => _error.ShouldBeOfExactType<Storage.EventSequences.EventPublicationConflict>();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_addressed_under_a_mismatching_key;

public class and_clearing_failed_partitions : given.an_observer_under_a_mismatching_key
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(_observer.ClearFailedPartitions);

    [Fact] void should_reject_the_operation() => _error.ShouldBeOfExactType<ObserverKeyDoesNotMatchDefinition>();
    [Fact] void should_name_the_event_sequence_of_the_definition() => _error!.Message.ShouldContain(_definitionEventSequenceId.Value);
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_write_the_definition() => _definitionStorageStats.Writes.ShouldEqual(0);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGrainStorageProvider;

public class when_writing_state_for_a_key_on_another_event_sequence_than_the_definition : given.the_provider
{
    static readonly ObserverKey _observerKey = new("an-observer", "an-event-store", "a-namespace", EventSequenceId.Log);
    static Exception? _error;

    void Establish() => _observerDefinitionsStorage.Get(_observerKey.ObserverId).Returns(ObserverDefinition.Empty with { EventSequenceId = "inbox-another-store" });

    async Task Because() => _error = await Catch.Exception(() => provider.WriteStateAsync(
        "name",
        GrainId.Create("type", _observerKey.ToString()),
        new GrainState<FailedPartitions> { State = new() { Partitions = [new() { Partition = "partition1" }] } }));

    [Fact] void should_refuse_the_write() => _error.ShouldBeOfExactType<ObserverKeyDoesNotMatchDefinition>();
    [Fact] async Task should_not_save_the_failed_partitions() => await _failedPartitionsStorage.DidNotReceive().Save(Arg.Any<ObserverId>(), Arg.Any<FailedPartitions>());
}

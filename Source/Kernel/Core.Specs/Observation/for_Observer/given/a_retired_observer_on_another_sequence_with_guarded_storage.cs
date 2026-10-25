// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_retired_observer_on_another_sequence_with_guarded_storage : an_observer
{
    protected ObserverDefinition _storedDefinition;
    protected FailedPartitions _storedFailures;

    protected override void ConfigureStorage(TestKitSilo silo, ObserverState persistedState)
    {
        _storedDefinition = _definitionStorage.State with { };
        _storedFailures = new() { Partitions = _failedPartitionsStorage.State.Partitions.ToArray() };
        _eventStoreStorage.Observers.Get(_observerId).Returns(_ => _storedDefinition);
        _eventStoreStorage.Observers.Save(Arg.Any<ObserverDefinition>()).Returns(call =>
        {
            _storedDefinition = call.Arg<ObserverDefinition>() with { };
            return Task.CompletedTask;
        });
        _eventStoreNamespaceStorage.FailedPartitions.GetFor(_observerId).Returns(_ => new FailedPartitions { Partitions = _storedFailures.Partitions.ToArray() });
        _eventStoreNamespaceStorage.FailedPartitions.Save(_observerId, Arg.Any<FailedPartitions>()).Returns(call =>
        {
            _storedFailures = new() { Partitions = call.Arg<FailedPartitions>().Partitions.ToArray() };
            return Task.CompletedTask;
        });

        var grainId = GrainId.Create("observer", _observerKey.ToString());
        var definitions = new provider_backed_storage<ObserverDefinition>(new ObserverDefinitionGrainStorageProvider(_storage), grainId);
        var failures = new provider_backed_storage<FailedPartitions>(new FailedPartitionGrainStorageProvider(_storage), grainId);
        silo.Options.StorageFactory = type =>
        {
            if (type == typeof(ObserverDefinition)) return definitions;
            if (type == typeof(FailedPartitions)) return failures;
            return null!;
        };
    }

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { Identifier = _observerId, AlertDisposition = AlertDisposition.Retired };
        _definitionStorage.State = _definitionStorage.State with { EventSequenceId = (EventSequenceId)"another-sequence" };
        _failedPartitionsStorage.State = new() { Partitions = [new() { ObserverId = _observerId, Partition = "failed-partition" }] };
        await Crash();
        _eventStoreStorage.Observers.ClearReceivedCalls();
        _eventStoreNamespaceStorage.FailedPartitions.ClearReceivedCalls();
    }
}

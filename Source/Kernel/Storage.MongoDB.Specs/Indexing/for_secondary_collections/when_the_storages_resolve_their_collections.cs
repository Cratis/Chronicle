// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB.Jobs;
using Cratis.Chronicle.Storage.MongoDB.Observation;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Compliance.MongoDB;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_secondary_collections;

[Collection(MongoDBCollection.Name)]
public class when_the_storages_resolve_their_collections(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    IReadOnlyList<string> _jobSteps;
    IReadOnlyList<string> _failedJobSteps;
    IReadOnlyList<string> _failedPartitions;
    IReadOnlyList<string> _inFlightEvents;
    IReadOnlyList<string> _encryptionKeys;

    async Task Because()
    {
        var eventStoreDatabase = Substitute.For<IEventStoreDatabase>();
        eventStoreDatabase.GetNamespaceDatabase(_namespace).Returns(_database);
        var rootDatabase = Substitute.For<IDatabase>();
        rootDatabase.GetEventStoreDatabase(_eventStore).Returns(eventStoreDatabase);

        await new JobStepStorage(_database).GetForJob(JobId.New());
        await new FailedPartitionStorage(_database).GetFor((ObserverId?)null);
        await new InFlightEventsStorage(_database).GetFor(ObserverId.Unspecified);
        await new EncryptionKeyStorage(rootDatabase).HasFor(_eventStore, _namespace, "some-identifier");

        _jobSteps = await IndexNamesFor(WellKnownCollectionNames.JobSteps);
        _failedJobSteps = await IndexNamesFor(WellKnownCollectionNames.FailedJobSteps);
        _failedPartitions = await IndexNamesFor(WellKnownCollectionNames.FailedPartitions);
        _inFlightEvents = await IndexNamesFor(WellKnownCollectionNames.InFlightEvents);
        _encryptionKeys = await IndexNamesFor("encryption-keys");
    }

    [Fact] void should_create_the_job_step_job_id_index() => _jobSteps.ShouldContain("jobId");
    [Fact] void should_create_the_failed_job_step_job_id_index() => _failedJobSteps.ShouldContain("jobId");
    [Fact] void should_create_the_failed_partition_observer_index() => _failedPartitions.ShouldContain("observerId");
    [Fact] void should_create_the_in_flight_events_index() => _inFlightEvents.ShouldContain("observerId_partition_eventSequenceNumber");
    [Fact] void should_create_the_encryption_key_index() => _encryptionKeys.ShouldContain("id_identifier_id_revision");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_CatchUpAdmission.given;

public class a_catch_up_admission : Specification
{
    protected CatchUpAdmission _admission;
    protected IJobStorage _jobStorage;

    protected static ObserverKey KeyOf(string observerId) => new(observerId, "some-event-store", "some-namespace", EventSequenceId.Log);

    protected static JobState CatchUpOf(string observerId, JobStatus status = JobStatus.Running) => new()
    {
        Id = JobId.New(),
        Status = status,
        Request = new CatchUpObserverRequest(KeyOf(observerId), ObserverType.Reactor, EventSequenceNumber.First, [])
    };

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _jobStorage = Substitute.For<IJobStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);
        namespaceStorage.Jobs.Returns(_jobStorage);
        RunningJobs();
        _admission = new CatchUpAdmission(storage);
    }

    protected void RunningJobs(params JobState[] jobs) =>
        _jobStorage.GetJobs(Arg.Any<JobQuery>()).Returns(_ => Task.FromResult(Catch<IImmutableList<JobState>>.Success(jobs.ToImmutableList())));
}

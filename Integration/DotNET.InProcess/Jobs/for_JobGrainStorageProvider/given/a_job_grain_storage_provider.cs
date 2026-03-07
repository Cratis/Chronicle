// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using ConceptsEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using ConceptsEventStoreNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;

namespace Cratis.Chronicle.InProcess.Integration.Jobs.for_JobGrainStorageProvider.given;

public class a_job_grain_storage_provider(ChronicleInProcessFixture fixture) : Specification(fixture)
{
    protected JobGrainStorageProvider _provider = default!;
    protected JobKey _jobKey = default!;

    protected void Establish()
    {
        var storage = Services.GetRequiredService<Storage.IStorage>();
        _provider = new JobGrainStorageProvider(storage);
        _jobKey = new JobKey(new ConceptsEventStoreName(EventStore.Name.Value), new ConceptsEventStoreNamespaceName(EventStore.Namespace.Value));
    }

    protected GrainId CreateGrainId(Guid guidValue)
    {
        var grainId = GrainIdKeyExtensions.CreateGuidKey(guidValue, _jobKey.ToString());
        var grainType = GrainType.Create("job");
        return GrainId.Create(grainType, grainId);
    }

    protected CatchUpObserverRequest CreateCatchUpObserverRequest()
    {
        var observerKey = new ObserverKey(
            (ObserverId)"test-observer",
            new ConceptsEventStoreName(EventStore.Name.Value),
            new ConceptsEventStoreNamespaceName(EventStore.Namespace.Value),
            EventSequenceId.Log);

        return new CatchUpObserverRequest(
            observerKey,
            ObserverType.Reactor,
            EventSequenceNumber.First,
            [new EventType((EventTypeId)"TestEvent", EventTypeGeneration.First)]);
    }
}

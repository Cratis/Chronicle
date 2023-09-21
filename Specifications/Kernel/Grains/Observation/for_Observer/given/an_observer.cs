// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.given;

public class an_observer : GrainSpecification<ObserverState>
{
    protected Observer observer;
    protected Mock<IEventSequenceStorage> event_sequence_storage;
    protected Mock<IStreamProvider> stream_provider;
    protected Mock<IStreamProvider> sequence_stream_provider;
    protected Mock<IObserverSubscriber> subscriber;
    protected ObserverKey ObserverKey => new(MicroserviceId.Unspecified, TenantId.NotSet, EventSequenceId.Log);

    protected override Guid GrainId => Guid.Parse("d2a138a2-6ca5-4bff-8a2f-ffd8534cc80e");

    protected override string GrainKeyExtension => ObserverKey;

    protected override Grain GetGrainInstance()
    {
        event_sequence_storage = new();
        observer = new Observer(() => event_sequence_storage.Object, Mock.Of<ILogger<Observer>>());
        return observer;
    }

    protected override void OnBeforeGrainActivate()
    {
        sequence_stream_provider = new();
        stream_provider_collection.Setup(_ => _.GetService(service_provider.Object, WellKnownProviders.EventSequenceStreamProvider)).Returns(sequence_stream_provider.Object);
        subscriber = new();
        grain_factory.Setup(_ => _.GetGrain(typeof(ObserverSubscriber), GrainId, IsAny<string>())).Returns(subscriber.Object);
    }

    protected override void OnAfterGrainActivate()
    {
        written_states.Clear();
    }
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.EventSequences;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.given;

public class an_observer : GrainSpecification<ObserverState>
{
    protected Observer observer;
    protected Mock<IStreamProvider> stream_provider;
    protected Mock<IEventSequence> event_sequence;
    protected Mock<IAsyncStream<AppendedEvent>> stream;
    protected EventLogSequenceNumberTokenWithFilter subscribed_token;

    protected override Grain GetGrainInstance()
    {
        observer = new Observer(Mock.Of<ILogger<Observer>>());
        return observer;
    }

    protected override void OnBeforeGrainActivate()
    {
        var key = new ObserverKey(MicroserviceId.Unspecified, TenantId.Development, EventSequenceId.Log).ToString();
        grain_identity.Setup(_ => _.GetPrimaryKey(out key)).Returns(Guid.NewGuid());

        event_sequence = new();
        grain_factory.Setup(_ => _.GetGrain<IEventSequence>(IsAny<Guid>(), IsAny<string>(), IsAny<string>())).Returns(event_sequence.Object);

        stream_provider = new();
        stream_provider_collection.Setup(_ => _.GetService(service_provider.Object, WellKnownProviders.EventSequenceStreamProvider)).Returns(stream_provider.Object);

        stream = new();
        stream_provider.Setup(_ => _.GetStream<AppendedEvent>(IsAny<Guid>(), IsAny<string>())).Returns(stream.Object);

        var subscription = new Mock<StreamSubscriptionHandle<AppendedEvent>>();
        stream.Setup(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()))
            .Returns((IAsyncObserver<AppendedEvent> _, StreamSequenceToken token, StreamFilterPredicate __, object ___) =>
            {
                subscribed_token = token as EventLogSequenceNumberTokenWithFilter;
                return Task.FromResult(subscription.Object);
            });
    }
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.EventSequences;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.given;

public class a_connected_observer : GrainSpecification<ObserverState>
{
    protected Observer observer;
    protected ObserverId observer_id;
    protected Mock<IStreamProvider> sequence_stream_provider;
    protected Mock<IStreamProvider> observer_stream_provider;
    protected Mock<IEventSequence> event_sequence;
    protected Mock<IAsyncStream<AppendedEvent>> sequence_stream;
    protected Mock<IAsyncStream<AppendedEvent>> observer_stream;
    protected EventLogSequenceNumberTokenWithFilter subscribed_token;
    protected List<EventLogSequenceNumberTokenWithFilter> subscribed_tokens;
    protected MicroserviceId microservice_id;
    protected TenantId tenant_id;
    protected EventSequenceId event_sequence_id;
    protected string connection_id;

    protected override Grain GetGrainInstance()
    {
        observer = new Observer(Mock.Of<ILogger<Observer>>());
        connection_id = Guid.NewGuid().ToString();
        observer.SetConnectionId(connection_id);
        return observer;
    }

    protected override void OnBeforeGrainActivate()
    {
        microservice_id = Guid.NewGuid();
        tenant_id = Guid.NewGuid();
        event_sequence_id = EventSequenceId.Log;
        var key = new ObserverKey(microservice_id, tenant_id, event_sequence_id).ToString();
        observer_id = Guid.NewGuid();
        grain_identity.Setup(_ => _.GetPrimaryKey(out key)).Returns(observer_id);

        event_sequence = new();
        grain_factory.Setup(_ => _.GetGrain<IEventSequence>(IsAny<Guid>(), IsAny<string>(), IsAny<string>())).Returns(event_sequence.Object);

        sequence_stream_provider = new();
        stream_provider_collection.Setup(_ => _.GetService(service_provider.Object, WellKnownProviders.EventSequenceStreamProvider)).Returns(sequence_stream_provider.Object);

        sequence_stream = new();
        sequence_stream_provider.Setup(_ => _.GetStream<AppendedEvent>(IsAny<Guid>(), IsAny<string>())).Returns(sequence_stream.Object);

        observer_stream_provider = new();
        stream_provider_collection.Setup(_ => _.GetService(service_provider.Object, WellKnownProviders.ObserverHandlersStreamProvider)).Returns(observer_stream_provider.Object);

        observer_stream = new();
        observer_stream_provider.Setup(_ => _.GetStream<AppendedEvent>(IsAny<Guid>(), IsAny<string>())).Returns(observer_stream.Object);

        subscribed_tokens = new();

        var subscription = new Mock<StreamSubscriptionHandle<AppendedEvent>>();
        sequence_stream.Setup(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()))
            .Returns((IAsyncObserver<AppendedEvent> _, StreamSequenceToken token, StreamFilterPredicate __, object ___) =>
            {
                subscribed_token = token as EventLogSequenceNumberTokenWithFilter;
                subscribed_tokens.Add(subscribed_token);
                return Task.FromResult(subscription.Object);
            });
    }
}

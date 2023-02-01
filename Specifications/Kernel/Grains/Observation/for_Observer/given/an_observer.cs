// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.EventSequences;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.given;

public class an_observer : GrainSpecification<ObserverState>
{
    protected Observer observer;
    protected ObserverId observer_id;
    protected Mock<IStreamProvider> sequence_stream_provider;
    protected Mock<IAsyncStream<AppendedEvent>> sequence_stream;
    protected EventSequenceNumberToken subscribed_token;
    protected List<EventSequenceNumberToken> subscribed_tokens;
    protected List<Mock<StreamSubscriptionHandle<AppendedEvent>>> subscription_handles;
    protected Mock<IEventSequenceStorageProvider> event_sequence_storage_provider;
    protected List<IAsyncObserver<AppendedEvent>> observers;
    protected MicroserviceId microservice_id;
    protected TenantId tenant_id;
    protected EventSequenceId event_sequence_id;
    protected Mock<IObserverSubscriber> subscriber;

    protected override Grain GetGrainInstance()
    {
        event_sequence_storage_provider = new();
        observer = new Observer(() => event_sequence_storage_provider.Object, Mock.Of<IExecutionContextManager>(), Mock.Of<ILogger<Observer>>());
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

        sequence_stream_provider = new();
        stream_provider_collection.Setup(_ => _.GetService(service_provider.Object, WellKnownProviders.EventSequenceStreamProvider)).Returns(sequence_stream_provider.Object);

        sequence_stream = new();
        sequence_stream_provider.Setup(_ => _.GetStream<AppendedEvent>(event_sequence_id.Value, new MicroserviceAndTenant(microservice_id, tenant_id))).Returns(sequence_stream.Object);

        subscribed_tokens = new();
        subscription_handles = new();
        observers = new();

        subscriber = new();
        subscriber.Setup(_ => _.OnNext(IsAny<AppendedEvent>())).Returns(Task.FromResult(ObserverSubscriberResult.Ok));
        grain_factory.Setup(_ => _.GetGrain(typeof(ObserverSubscriber), observer_id, IsAny<string>())).Returns(subscriber.Object);

        sequence_stream.Setup(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()))
            .Returns((IAsyncObserver<AppendedEvent> observer, StreamSequenceToken token, StreamFilterPredicate __, object ___) =>
            {
                var subscription = new Mock<StreamSubscriptionHandle<AppendedEvent>>();
                subscription_handles.Add(subscription);
                subscribed_token = token as EventSequenceNumberToken;
                subscribed_tokens.Add(subscribed_token);
                observers.Add(observer);

                return Task.FromResult(subscription.Object);
            });
    }
}

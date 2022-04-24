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
    protected ObserverId observer_id;
    protected Mock<IStreamProvider> sequence_stream_provider;
    protected Mock<IStreamProvider> observer_stream_provider;
    protected Mock<IAsyncStream<AppendedEvent>> sequence_stream;
    protected Mock<IAsyncStream<AppendedEvent>> observer_stream;
    protected EventSequenceNumberTokenWithFilter subscribed_token;
    protected List<EventSequenceNumberTokenWithFilter> subscribed_tokens;
    protected List<Mock<StreamSubscriptionHandle<AppendedEvent>>> subscription_handles;
    protected Mock<IEventSequenceStorageProvider> event_sequence_storage_provider;
    protected List<IAsyncObserver<AppendedEvent>> observers;
    protected MicroserviceId microservice_id;
    protected TenantId tenant_id;
    protected EventSequenceId event_sequence_id;
    protected ObserverNamespace observer_namespace;

    protected override Grain GetGrainInstance()
    {
        event_sequence_storage_provider = new();
        observer = new Observer(() => event_sequence_storage_provider.Object, Mock.Of<IExecutionContextManager>(), Mock.Of<ILogger<Observer>>());
        observer_namespace = Guid.NewGuid().ToString();
        return observer;
    }

    protected override void OnBeforeGrainActivate()
    {
        state.CurrentNamespace = observer_namespace;

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

        observer_stream_provider = new();
        stream_provider_collection.Setup(_ => _.GetService(service_provider.Object, WellKnownProviders.ObserverHandlersStreamProvider)).Returns(observer_stream_provider.Object);

        observer_stream = new();
        observer_stream_provider.Setup(_ => _.GetStream<AppendedEvent>(observer_id, observer_namespace.Value)).Returns(observer_stream.Object);

        subscribed_tokens = new();
        subscription_handles = new();
        observers = new();

        sequence_stream.Setup(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()))
            .Returns((IAsyncObserver<AppendedEvent> observer, StreamSequenceToken token, StreamFilterPredicate __, object ___) =>
            {
                var subscription = new Mock<StreamSubscriptionHandle<AppendedEvent>>();
                subscription_handles.Add(subscription);
                subscribed_token = token as EventSequenceNumberTokenWithFilter;
                subscribed_tokens.Add(subscribed_token);
                observers.Add(observer);

                return Task.FromResult(subscription.Object);
            });
    }
}

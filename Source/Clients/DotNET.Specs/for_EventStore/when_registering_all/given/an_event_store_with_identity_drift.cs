// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Serialization;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_registering_all.given;

public class an_event_store_with_identity_drift : an_event_store_with_a_projection_that_cannot_be_built
{
    protected static readonly ArtifactIdentityDrift ExpectedDrift = new(ArtifactKind.Reactor, "Original.SendEmail", "Renamed.SendEmail");
    protected Exception? _exception;
    private protected DiscoveryReportBuilder _builder;

    async Task Establish()
    {
        var eventTypes = _eventStore.EventTypes;
        eventTypes.All.Returns([]);
        var connection = Substitute.For<Connections.IChronicleConnection, Contracts.IChronicleServicesAccessor>();
        connection.Lifecycle.Returns(_connectionLifecycle);
        ((Contracts.IChronicleServicesAccessor)connection).Services.Returns(_servicesAccessor.Services);
        var store = Substitute.For<IEventStore>();
        store.Connection.Returns(connection);
        store.Name.Returns(_eventStore.Name);
        store.Namespace.Returns(_eventStore.Namespace);
        _servicesAccessor.Services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()).Returns(Observable.Never<EventsToObserve>());
        var reactors = new Reactors.Reactors(
            store,
            eventTypes,
            _clientArtifacts,
            Substitute.For<IServiceProvider>(),
            Substitute.For<IClientArtifactsActivator>(),
            Substitute.For<IActivateReactorMiddlewares>(),
            Substitute.For<IEventSerializer>(),
            Substitute.For<ICausationManager>(),
            Substitute.For<IIdentityProvider>(),
            Substitute.For<IActivitySource<Reactors.Reactors>>(),
            Substitute.For<IReactorSideEffectHandlers>(),
            Substitute.For<IReactorContextValuesBuilder>(),
            new ReactorMethodArgumentsResolver(),
            NullLogger<Reactors.Reactors>.Instance,
            NullLoggerFactory.Instance);
        await reactors.Register("Renamed.SendEmail", builder => builder.WithEventType(new("SomeEvent", EventTypeGeneration.First)), (_, _) => Task.CompletedTask);
        _builder = new(eventTypes, new Constraints(store, []), reactors, _eventStore.Reducers, (Projections.Projections)_projections, new DefaultNamingPolicy());
        SetField("_discoveryReportBuilder", _builder);
        SetField("_identityDriftDetector", new ArtifactIdentityDriftDetector(_servicesAccessor, _eventStore.Name, _eventStore.Namespace, eventTypes, NullLogger.Instance));
        _servicesAccessor.Services.Observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<CallContext>()).Returns([
            new ObserverInformation { Id = "Original.SendEmail", Type = ObserverType.Reactor, Owner = ObserverOwner.Client, EventSequenceId = "event-log" }
        ]);
        _servicesAccessor.Services.EventTypes.AllEventTypes(Arg.Any<AllEventTypesRequest>(), Arg.Any<CallContext>()).Returns(QueryResult<IEnumerable<EventTypeDetailsResponse>>.Success(Guid.NewGuid(), []));
        _servicesAccessor.Services.Observers.GetObserverInformation(Arg.Any<GetObserverInformationRequest>(), Arg.Any<CallContext>()).Returns(new ObserverInformation { IsSubscribed = false });
    }
}

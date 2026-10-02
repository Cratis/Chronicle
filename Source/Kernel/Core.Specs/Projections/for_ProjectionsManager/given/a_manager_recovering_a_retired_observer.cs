// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.ReadModels;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.given;

public class a_manager_recovering_a_retired_observer : Observation.for_Observer.given.an_observer
{
    protected TestKitSilo _managerSilo;
    protected ProjectionsManager _manager;
    protected ProjectionDefinition _projectionDefinition;
    protected Guid _retiredLifecycle;
    protected IObserver _managedObserver;

    async Task Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        await _observer.TransitionTo<QuarantinedObserver>();
        FailAlertReports(new TimeoutException());
        await Catch.Exception(_observer.Retire);
        _retiredLifecycle = _stateStorage.State.AlertLifecycleId;
        await Crash();
        ApplyAlertReports();
        _jobsManager.ClearReceivedCalls();

        _managerSilo = new TestKitSilo();
        var comparer = Substitute.For<IProjectionDefinitionComparer>();
        comparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>()).Returns(ProjectionDefinitionCompareResult.Same);
        _managerSilo.AddService(comparer);
        var engineProjection = Substitute.For<Engine.IProjection>();
        engineProjection.EventTypes.Returns([]);
        engineProjection.IsEventSourceKeyed.Returns(true);
        var factory = Substitute.For<IProjectionFactory>();
        factory.Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>()).Returns(engineProjection);
        _managerSilo.AddService(factory);
        var service = Substitute.For<IProjectionsServiceClient>();
        service.Register(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<ProjectionDefinition>>())
            .Returns(Cratis.Monads.Result<ProjectionRegistrationError>.Success());
        _managerSilo.AddService(service);
        _managerSilo.AddService(Substitute.For<ILanguageService>());
        _managerSilo.AddService(Substitute.For<ILocalSiloDetails>());
        _managerSilo.AddService(_storage);
        _eventStoreNamespaceStorage.Observers.GetRetired(Arg.Any<IEnumerable<ObserverId>>())
            .Returns(call => call.Arg<IEnumerable<ObserverId>>()
                .Where(id => id == _observerId && _stateStorage.State.AlertDisposition == AlertDisposition.Retired).ToArray());
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([_observerKey.Namespace]);
        _managerSilo.AddProbe(_ => namespaces);
        var readModels = Substitute.For<IReadModelsManager>();
        readModels.GetDefinitions().Returns([a_projections_manager_grain.CreateReadModelDefinition("read-model")]);
        _managerSilo.AddProbe(_ => readModels);
        _managerSilo.AddProbe(_ => Substitute.For<IProjection>());
        _managedObserver = Substitute.For<IObserver>();
        _managedObserver.IsSubscribed().Returns(_ => _observer.IsSubscribed());
        _managedObserver.Subscribe<IProjectionObserverSubscriber>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), reactivateRetired: Arg.Any<bool>())
            .Returns(call => _observer.Subscribe<IProjectionObserverSubscriber>(ObserverType.Projection, call.Arg<IEnumerable<EventType>>(), SiloAddress.Zero, reactivateRetired: call.ArgAt<bool>(6)));
        _managedObserver.SubscribeToAllEvents<IProjectionObserverSubscriber>(Arg.Any<ObserverType>(), Arg.Any<SiloAddress>(), reactivateRetired: Arg.Any<bool>())
            .Returns(call => _observer.SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, SiloAddress.Zero, reactivateRetired: call.ArgAt<bool>(4)));
        _managerSilo.AddProbe(_ => _managedObserver);
        _projectionDefinition = a_projections_manager_grain.CreateDefinition(_observerId.Value, "read-model");
        var state = _managerSilo.StorageManager.GetStorage<ProjectionsManagerState>(typeof(ProjectionsManager).FullName);
        state.State = new ProjectionsManagerState { Projections = [_projectionDefinition] };
        _manager = await _managerSilo.CreateGrainAsync<ProjectionsManager>(_observerKey.EventStore.Value);
    }
}

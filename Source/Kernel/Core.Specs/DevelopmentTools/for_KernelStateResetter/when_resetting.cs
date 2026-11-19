// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStores;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Setup.Authentication;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.DevelopmentTools.for_KernelStateResetter;

public class when_resetting : Specification
{
    readonly List<string> _operations = [];
    readonly List<EventStoreName> _eventStores = [new("target")];
    bool _recreatedEventStoreExists;
    IGrainFactory _grainFactory;
    IStorage _storage;
    IEventStoreSubscriptionsManager _manager;
    IInstancesOf<ICanPerformKernelStateReset> _handlers;
    KernelBootstrapResetHandler _bootstrap;
    IKernelStateResetter _resetter;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        var management = GrainWithIdentity<IManagementGrain>("management");
        _grainFactory.GetGrain<IManagementGrain>(0).Returns(management);
        var managerId = GrainId.Create("subscriptions", "target");
        management.GetDetailedGrainStatistics().Returns([new DetailedGrainStatistic { GrainId = managerId }]);
        management.GetTotalActivationCount().Returns(1);
        management.ForceActivationCollection(TimeSpan.Zero).Returns(_ =>
        {
            _operations.Add("deactivate");
            return Task.CompletedTask;
        });
        var queue = GrainWithIdentity<IAppendedEventsQueue>("queue");
        var queues = GrainWithIdentity<IAppendedEventsQueues>("queues");
        var observer = GrainWithIdentity<IObserver>("observer");
        _grainFactory.GetGrain<IAppendedEventsQueue>(0, "reset-type-lookup").Returns(queue);
        _grainFactory.GetGrain<IAppendedEventsQueues>("reset-type-lookup").Returns(queues);
        _grainFactory.GetGrain<IObserver>("reset-type-lookup").Returns(observer);
        _storage = CreateStorage();
        _storage.GetEventStore("target");
        var subscriptions = GrainWithIdentity<IEventStoreSubscriptionsManager>("subscriptions");
        _grainFactory.GetGrain<IEventStoreSubscriptionsManager>("reset-type-lookup").Returns(subscriptions);
        _manager = Substitute.For<IEventStoreSubscriptionsManager>();
        _grainFactory.GetGrain<IEventStoreSubscriptionsManager>("target").Returns(_manager);
        _manager.GetSubscriptionDefinitions().Returns([new EventStoreSubscriptionDefinition("subscription", "source", [])]);
        _manager.Remove(Arg.Any<EventStoreSubscriptionId>()).Returns(_ =>
        {
            _operations.Add("remove-subscription");
            return Task.CompletedTask;
        });
        var handler = Substitute.For<ICanPerformKernelStateReset>();
        handler.CanReset().Returns(true);
        handler.Reset().Returns(_ =>
        {
            _operations.Add("wipe");
            _eventStores.Clear();
            return Task.CompletedTask;
        });
        _handlers = Substitute.For<IInstancesOf<ICanPerformKernelStateReset>>();
        _handlers.GetEnumerator().Returns(_ => ((IEnumerable<ICanPerformKernelStateReset>)[handler]).GetEnumerator());
        var namespaces = Substitute.For<INamespaces>();
        _grainFactory.GetGrain<INamespaces>(EventStoreName.System).Returns(namespaces);
        _bootstrap = new KernelBootstrapResetHandler(_grainFactory, Substitute.For<IEventTypes>(), Substitute.For<IReactors>(), new TestAuthenticationService());
        _resetter = new KernelStateResetter(_grainFactory, _storage, Substitute.For<IProjectionPipelineManager>(), _handlers, Substitute.For<IEventStoreInitialization>(), _bootstrap, Microsoft.Extensions.Logging.Abstractions.NullLogger<KernelStateResetter>.Instance);
    }

    async Task Because()
    {
        await _resetter.Reset();
        _storage.GetEventStore("target");
        _recreatedEventStoreExists = await _storage.HasEventStore("target");
    }

    IStorage CreateStorage()
    {
        var cluster = Substitute.For<IClusterStorage>();
        cluster.GetEventStores().Returns(_ => Task.FromResult<IEnumerable<EventStoreName>>(_eventStores.ToArray()));
        cluster.CreateStorageForEventStore(Arg.Any<EventStoreName>(), Arg.Any<SinksFactory>()).Returns(_ => Substitute.For<IEventStoreStorage>());
        cluster.SaveEventStore(Arg.Any<EventStoreName>()).Returns(call =>
        {
            _eventStores.Add(call.Arg<EventStoreName>());
            return Task.CompletedTask;
        });
        return new Storage.Storage(cluster, Substitute.For<ISystemStorage>(), Substitute.For<IInstancesOf<Storage.Sinks.ISinkFactory>>());
    }

    static T GrainWithIdentity<T>(string type)
        where T : class, IGrain
    {
        var grain = Substitute.For<T, IGrainBase>();
        var context = Substitute.For<IGrainContext>();
        context.GrainId.Returns(GrainId.Create(type, "lookup"));
        ((IGrainBase)grain).GrainContext.Returns(context);
        return grain;
    }

    sealed class TestAuthenticationService : IAuthenticationService
    {
        public Task<Storage.Security.User?> AuthenticateUser(Concepts.Security.Username username, Concepts.Security.Password password) => Task.FromResult<Storage.Security.User?>(null);
        public Task EnsureDefaultAdminUser() => Task.CompletedTask;
        public Task EnsureBootstrapClients() => Task.CompletedTask;
        public Task EnsureDefaultClientCredentials() => Task.CompletedTask;
    }

    [Fact] void should_recreate_a_previously_cached_event_store_in_the_wiped_catalog() => _recreatedEventStoreExists.ShouldBeTrue();
    [Fact] async Task should_remove_the_subscription() => await _manager.Received(1).Remove(new EventStoreSubscriptionId("subscription"));
    [Fact] void should_wipe_before_and_after_activation_collection() => _operations.ShouldEqual(["remove-subscription", "wipe", "deactivate", "wipe"]);
}
#endif

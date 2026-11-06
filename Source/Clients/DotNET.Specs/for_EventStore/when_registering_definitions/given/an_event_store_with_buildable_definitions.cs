// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Serverless;
using Cratis.Monads;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions.given;

public class an_event_store_with_buildable_definitions : Specification
{
    protected EventStore _eventStore;
    protected IServices _services;
    protected IChronicleServicesAccessor _servicesAccessor;
    protected IClientArtifactsProvider _clientArtifacts;
    protected DefinitionsRegistration _result;
    protected Exception _failure;
    protected Projections.Projections _projections;

    void Establish()
    {
        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _clientArtifacts.ModelBoundProjections.Returns([]);
        _clientArtifacts.Reactors.Returns([]);
        _clientArtifacts.Reducers.Returns([]);
        var activator = Substitute.For<IClientArtifactsActivator>();
        activator.ActivateNonDisposable<IProjectionFor<BuildableModel>>(typeof(BuildableProjection)).Returns(Catch<IProjectionFor<BuildableModel>>.Success(new BuildableProjection()));
        _services = Substitute.For<IServices>();
        _services.EventStores.EnsureEventStore(Arg.Any<Contracts.EventStores.EnsureEventStoreRequest>()).Returns(new CommandResult());
        _services.EventStores.RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>()).Returns(new CommandResult());
        _servicesAccessor = Substitute.For<IChronicleServicesAccessor>();
        _servicesAccessor.Services.Returns(_services);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(_services);
        var projectionStore = Substitute.For<IEventStore>();
        projectionStore.Connection.Returns(connection);
        projectionStore.Name.Returns(new EventStoreName("Testing"));
        _projections = new(projectionStore, Substitute.For<IEventTypes>(), _clientArtifacts, new DefaultNamingPolicy(), activator, new JsonSerializerOptions(), NullLogger<Projections.Projections>.Instance);
        _projections.Discover().GetAwaiter().GetResult();
        _eventStore = (EventStore)RuntimeHelpers.GetUninitializedObject(typeof(EventStore));
        var options = new ChronicleOptions();
        SetField("_chronicleOptions", options);
        SetAutoProperty(nameof(EventStore.Deliveries), new ObserverDeliveries(options));
        SetField("_eventStoreName", new EventStoreName("Testing"));
        SetField("_servicesAccessor", _servicesAccessor);
        SetField("_clientArtifactsProvider", _clientArtifacts);
        SetField("_projections", _projections);
        SetField("_registrationRetry", new RegistrationRetryOptions { MaxAttempts = 1 });
        SetField("_registrationBackoff", new RegistrationBackoff(TimeSpan.Zero, TimeSpan.Zero));
        SetField("_registration", RegistrationOutcome.NotRun);
        SetField("_logger", Substitute.For<ILogger<EventStore>>());
        connection.Lifecycle.Returns(new ConnectionLifecycle(Substitute.For<ILogger<ConnectionLifecycle>>()));
        SetAutoProperty(nameof(EventStore.Connection), connection);
        SetAutoProperty(nameof(EventStore.Name), new EventStoreName("Testing"));
        SetAutoProperty(nameof(EventStore.Namespace), EventStoreNamespaceName.Default);
        SetAutoProperty(nameof(EventStore.Projections), _projections);
        SetAutoProperty(nameof(EventStore.EventTypes), Substitute.For<IEventTypes>());
        SetAutoProperty(nameof(EventStore.EventSources), Substitute.For<IEventSources>());
        SetAutoProperty(nameof(EventStore.ReadModels), Substitute.For<IReadModels>());
        SetAutoProperty(nameof(EventStore.Constraints), Substitute.For<IConstraints>());
        SetAutoProperty(nameof(EventStore.Reactors), Substitute.For<Reactors.IReactors>());
        SetAutoProperty(nameof(EventStore.Reducers), Substitute.For<IReducers>());
        SetAutoProperty(nameof(EventStore.ReadModelReactors), Substitute.For<IReadModelReactors>());
        SetAutoProperty(nameof(EventStore.Subscriptions), Substitute.For<IEventStoreSubscriptions>());
        SetAutoProperty(nameof(EventStore.Seeding), Substitute.For<IEventSeeding>());
    }

    protected void SetField(string fieldName, object value) =>
        (typeof(EventStore).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException(fieldName)).SetValue(_eventStore, value);

    protected void SetAutoProperty(string name, object value) => SetField($"<{name}>k__BackingField", value);

    public record BuildableModel();

    public class BuildableProjection : IProjectionFor<BuildableModel>
    {
        public void Define(IProjectionBuilderFor<BuildableModel> builder)
        {
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Seeding;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.for_EventStore.when_registering_all.given;

/// <summary>
/// Builds an event store around a real <see cref="Chronicle.Projections.Projections"/> that has discovered one
/// projection it could build and one it could not, with every other artifact system substituted. Constructing the real
/// <see cref="EventStore"/> would drag in the whole client, so the fields are set the way the other event store
/// specifications set them.
/// </summary>
public class an_event_store_with_a_projection_that_cannot_be_built : Specification
{
    protected EventStore _eventStore;
    protected IProjections _projections;
    protected IClientArtifactsProvider _clientArtifacts;
    protected IChronicleServicesAccessor _servicesAccessor;
    protected ConnectionLifecycle _connectionLifecycle;

    /// <summary>
    /// The retry settings the event store registers under. A single attempt by default, so a specification about
    /// something other than retrying sees the first failure rather than the last.
    /// </summary>
    protected RegistrationRetryOptions _registrationRetry = new() { MaxAttempts = 1 };

    void Establish()
    {
        var clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        clientArtifacts.Projections.Returns([typeof(BuildableProjection), typeof(UnbuildableProjection)]);
        clientArtifacts.ModelBoundProjections.Returns([]);
        clientArtifacts.Reactors.Returns([]);
        clientArtifacts.Reducers.Returns([]);
        _clientArtifacts = clientArtifacts;

        var artifactsActivator = Substitute.For<IClientArtifactsActivator>();
        artifactsActivator
            .ActivateNonDisposable<IProjectionFor<BuildableModel>>(typeof(BuildableProjection))
            .Returns(new BuildableProjection());
        artifactsActivator
            .ActivateNonDisposable<IProjectionFor<UnbuildableModel>>(typeof(UnbuildableProjection))
            .Returns(new UnbuildableProjection());

        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var eventStoreForProjections = Substitute.For<IEventStore>();
        eventStoreForProjections.Connection.Returns(connection);
        eventStoreForProjections.Name.Returns(new EventStoreName("Testing"));

        var projections = new Projections.Projections(
            eventStoreForProjections,
            Substitute.For<IEventTypes>(),
            clientArtifacts,
            new DefaultNamingPolicy(),
            artifactsActivator,
            new JsonSerializerOptions(),
            NullLogger<Projections.Projections>.Instance);
        projections.Discover().GetAwaiter().GetResult();
        _projections = projections;

        _servicesAccessor = ServicesAccessorEnsuringTheEventStore();
        _eventStore = (EventStore)RuntimeHelpers.GetUninitializedObject(typeof(EventStore));
        SetField("_eventStoreName", new EventStoreName("Testing"));
        SetField("_clientArtifactsProvider", clientArtifacts);
        SetField("_servicesAccessor", _servicesAccessor);
        SetField("_logger", Substitute.For<ILogger<EventStore>>());
        SetField("_projections", projections);
        SetField("_registrationRetry", _registrationRetry);

        // Zero delays: a specification about retrying asserts on how many attempts were made, never on the clock.
        SetField("_registrationBackoff", new RegistrationBackoff(TimeSpan.Zero, TimeSpan.Zero));
        SetAutoProperty("Registration", Registrations.RegistrationOutcome.NotRun);

        // A real lifecycle (not a mock) so a specification can drive Disconnected()/Connected() directly and have
        // the event store's own OnDisconnected subscription observe it exactly as it would in production.
        _connectionLifecycle = new ConnectionLifecycle(Substitute.For<ILogger<ConnectionLifecycle>>());
        var eventStoreConnection = Substitute.For<IChronicleConnection>();
        eventStoreConnection.Lifecycle.Returns(_connectionLifecycle);
        SetAutoProperty("Connection", eventStoreConnection);
        SetAutoProperty("Name", new EventStoreName("Testing"));
        SetAutoProperty("Namespace", new EventStoreNamespaceName("default"));
        SetAutoProperty("EventTypes", Substitute.For<IEventTypes>());
        SetAutoProperty("ReadModels", Substitute.For<IReadModels>());
        SetAutoProperty("Constraints", Substitute.For<IConstraints>());
        SetAutoProperty("Reactors", Substitute.For<Reactors.IReactors>());
        SetAutoProperty("Reducers", Substitute.For<IReducers>());
        SetAutoProperty("Projections", projections);
        SetAutoProperty("ReadModelReactors", Substitute.For<IReadModelReactors>());
        SetAutoProperty("Subscriptions", Substitute.For<IEventStoreSubscriptions>());
        SetAutoProperty("Seeding", Substitute.For<IEventSeeding>());
    }

    /// <summary>
    /// Registration ensures the event store exists before anything else runs, and every command now answers with a
    /// <see cref="Contracts.Commands.CommandResult"/>. Left unstubbed the call answers with nothing at all, which
    /// fails registration before it reaches the projections these specifications are about.
    /// </summary>
    /// <returns>A <see cref="IChronicleServicesAccessor"/> whose event store command succeeds.</returns>
    static IChronicleServicesAccessor ServicesAccessorEnsuringTheEventStore()
    {
        var servicesAccessor = Substitute.For<IChronicleServicesAccessor>();
        servicesAccessor.Services.EventStores
            .EnsureEventStore(Arg.Any<Contracts.EventStores.EnsureEventStoreRequest>())
            .Returns(Contracts.Commands.CommandResult.Success(Guid.NewGuid()));
        return servicesAccessor;
    }

    static void SetField(EventStore eventStore, string fieldName, object value) =>
        typeof(EventStore).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(eventStore, value);

    void SetField(string fieldName, object value) => SetField(_eventStore, fieldName, value);

    void SetAutoProperty(string propertyName, object value) => SetField(_eventStore, $"<{propertyName}>k__BackingField", value);

    public record BuildableModel();

    public record UnbuildableModel();

    public class BuildableProjection : IProjectionFor<BuildableModel>
    {
        public void Define(IProjectionBuilderFor<BuildableModel> builder)
        {
        }
    }

    public class UnbuildableProjection : IProjectionFor<UnbuildableModel>
    {
        public void Define(IProjectionBuilderFor<UnbuildableModel> builder) => throw new ProjectionCannotBeDefined();
    }

    /// <summary>
    /// The exception that is thrown when a projection in this specification cannot define itself.
    /// </summary>
    public class ProjectionCannotBeDefined() : Exception("The projection cannot be defined");
}

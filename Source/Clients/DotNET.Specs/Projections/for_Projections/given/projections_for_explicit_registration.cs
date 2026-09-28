// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;

using ProjectionsService = Cratis.Chronicle.Contracts.Projections.IProjections;

namespace Cratis.Chronicle.Projections.for_Projections.given;

/// <summary>
/// A projections instance that discovered nothing, for an event store whose connection state each spec decides.
/// </summary>
public class projections_for_explicit_registration : all_dependencies
{
    protected Projections _projections;
    protected IConnectionLifecycle _lifecycle;
    protected ProjectionsService _projectionsService;
    protected IReadModels _readModels;
    protected List<RegisterRequest> _registrations = [];

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications([typeof(ItemAdded), typeof(ItemRemoved), typeof(CustomerRegistered)]);
        _eventStore.Name.Returns((EventStoreName)"test-event-store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"test-namespace");

        _projectionsService = Substitute.For<ProjectionsService>();
        _projectionsService
            .When(_ => _.Register(Arg.Any<RegisterRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()))
            .Do(call => _registrations.Add(call.Arg<RegisterRequest>()));
        var services = Substitute.For<IServices>();
        services.Projections.Returns(_projectionsService);
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        connection.Lifecycle.Returns(_lifecycle);
        _eventStore.Connection.Returns(connection);

        _readModels = Substitute.For<IReadModels>();
        _eventStore.ReadModels.Returns(_readModels);

        _clientArtifacts.Projections.Returns([]);
        _clientArtifacts.ModelBoundProjections.Returns([]);

        _projections = new Projections(
            _eventStore,
            _eventTypes,
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            NullLogger<Projections>.Instance);
    }

    [EventType]
    public record ItemAdded(string Name, int Quantity);

    [EventType]
    public record ItemRemoved(string Name);

    [EventType]
    public record CustomerRegistered(string Name);

    public record Inventory(string Name, int Quantity);

    public record Customer(string Name);

    [FromEvent<CustomerRegistered>]
    public record RegisteredCustomer(string Name);

    [FromEvent<CustomerRegistered>]
    public readonly record struct RegisteredCustomerStruct(string Name);

    public record NotModelBound(string Name);
}

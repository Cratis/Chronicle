// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Microsoft.Extensions.Logging.Abstractions;
using ClientFailedPartitions = Cratis.Chronicle.Observation.IFailedPartitions;

namespace Cratis.Chronicle.Projections.for_Projections.given;

/// <summary>
/// A projections instance that discovered a model-bound projection and no fluent one, so the read model type is the
/// only handle anything here can hold.
/// </summary>
public class a_discovered_model_bound_projection : all_dependencies
{
    protected Projections _projections;
    protected IObservers _observers;
    protected ClientFailedPartitions _failedPartitions;

    async Task Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"test-event-store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"test-namespace");

        _observers = Substitute.For<IObservers>();
        var services = Substitute.For<IServices>();
        services.Observers.Returns(_observers);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        _eventStore.Connection.Returns(connection);

        _failedPartitions = Substitute.For<ClientFailedPartitions>();
        _eventStore.FailedPartitions.Returns(_failedPartitions);

        _clientArtifacts.Projections.Returns([]);
        _clientArtifacts.ModelBoundProjections.Returns([typeof(TheModelBoundReadModel)]);

        _projections = new Projections(
            _eventStore,
            _eventTypes,
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            NullLogger<Projections>.Instance);

        await _projections.Discover();
    }

    public record TheModelBoundReadModel(string Id, string Name);
}

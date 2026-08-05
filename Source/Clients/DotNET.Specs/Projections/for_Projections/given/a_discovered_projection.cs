// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Microsoft.Extensions.Logging.Abstractions;
using ClientFailedPartitions = Cratis.Chronicle.Observation.IFailedPartitions;

namespace Cratis.Chronicle.Projections.for_Projections.given;

public class a_discovered_projection : all_dependencies
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

        _clientArtifacts.Projections.Returns([typeof(TheProjection)]);
        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<TheReadModel>>(typeof(TheProjection))
            .Returns(new TheProjection());

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

    public record TheReadModel(string Id);

    public class TheProjection : IProjectionFor<TheReadModel>
    {
        public void Define(IProjectionBuilderFor<TheReadModel> builder)
        {
        }
    }
}

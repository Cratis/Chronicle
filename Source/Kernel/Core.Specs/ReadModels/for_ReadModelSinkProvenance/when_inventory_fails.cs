// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance;

public class when_inventory_fails : given.a_provenance_inventory
{
    readonly InvalidOperationException _failure = new("unavailable");
    Exception? _readError;
    Exception? _mutationError;
    Exception? _retryError;

    void Establish() => _eventStore.ReadModels.GetAll().Returns(_ => Task.FromException<IEnumerable<ReadModelDefinition>>(_failure));

    async Task Because()
    {
        _readError = await Catch.Exception(() => _provenance.EnsureReadable(Store, EventStoreNamespaceName.Default, _model));
        _mutationError = await Catch.Exception(() => _provenance.Preserve(Store));
        _projections = [_projection with { EventSequenceId = "system" }];
        _eventStore.ReadModels.GetAll().Returns(_ => Task.FromResult<IEnumerable<ReadModelDefinition>>(_models));
        var marker = Substitute.For<ISinkProvenanceStorage>();
        marker.MarkQuarantined().Returns(Task.FromException(_failure));
        _markers[(EventStoreNamespaceName.Default, _model.Sink.Configuration)] = marker;
        _retryError = await Catch.Exception(() => _provenance.EnsureReadable(Store, EventStoreNamespaceName.Default, _model));
    }

    [Fact] void should_refuse_the_read_on_inventory_failure() => _readError.ShouldEqual(_failure);
    [Fact] void should_refuse_the_mutation_on_inventory_failure() => _mutationError.ShouldEqual(_failure);
    [Fact] void should_retry_inventory_and_propagate_a_persistence_failure() => _retryError.ShouldEqual(_failure);
}

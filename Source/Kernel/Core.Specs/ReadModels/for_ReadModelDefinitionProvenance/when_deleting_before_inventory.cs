// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.ReadModels.for_ReadModelDefinitionProvenance;

public class when_deleting_before_inventory : Projections.for_ProjectionsManager.given.a_projections_manager_grain
{
    readonly InvalidOperationException _failure = new("unavailable");
    readonly List<Exception?> _errors = [];
    Storage.IStorage _storage;
    ReadModelsManagerState _pending;

    protected override IReadModelSinkProvenance CreateProvenance(Storage.IStorage storage)
    {
        _storage = storage;
        return new ReadModelSinkProvenance(storage);
    }

    void Establish()
    {
        _eventStoreStorage.ReadModels.GetAll().Returns(Task.FromException<IEnumerable<ReadModelDefinition>>(_failure));
        _eventStoreStorage.ReadModels.Get("model").Returns(CreateReadModelDefinition("model"));
        _observerGrain.ClearReceivedCalls();
    }

    async Task Because()
    {
        _errors.Add(await Catch.Exception(() => _grain.Remove("projection")));
        var projection = CreateDefinition("projection", "model");
        var projectionState = new GrainState<ProjectionDefinition> { State = projection };
        var projectionKey = GrainId.Create("projection", new ProjectionKey(projection.Identifier, EventStore).ToString());
        var projections = new ProjectionDefinitionStorageProvider(_storage, TimeProvider.System, _provenance);
        _errors.Add(await Catch.Exception(() => projections.ClearStateAsync("projection", projectionKey, projectionState)));
        _errors.Add(await Catch.Exception(() => projections.WriteStateAsync("projection", projectionKey, projectionState)));
        _pending = new() { ReadModels = [CreateReadModelDefinition("model")], Modified = [CreateReadModelDefinition("model")] };
        var models = new ReadModelsManagerStorageProvider(_storage, _provenance);
        _errors.Add(await Catch.Exception(() => models.WriteStateAsync("read-models", GrainId.Create("read-models", EventStore), new GrainState<ReadModelsManagerState> { State = _pending })));
        var reducers = new ReducerDefinitionStorageProvider(_storage, _provenance);
        var reducer = new ReducerDefinition("reducer", "log", [], "model", true, []);
        var reducerKey = GrainId.Create("reducer", new ReducerKey(reducer.Identifier, EventStore).ToString());
        _errors.Add(await Catch.Exception(() => reducers.WriteStateAsync("reducer", reducerKey, new GrainState<ReducerDefinition> { State = reducer })));
    }

    [Fact] void should_refuse_every_evidence_changing_operation() => _errors.All(error => ReferenceEquals(error, _failure)).ShouldBeTrue();
    [Fact] void should_not_call_any_delete_or_save() => _eventStoreStorage.Projections.ReceivedCalls().Concat(_eventStoreStorage.ReadModels.ReceivedCalls()).Concat(_eventStoreStorage.Reducers.ReceivedCalls()).Any(call => call.GetMethodInfo().Name is "Delete" or "Save").ShouldBeFalse();
    [Fact] void should_not_append_projection_history() => _eventStoreStorage.ProjectionDefinitionHistory.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_stop_or_remove_an_observer() => _observerGrain.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_retain_pending_read_model_changes() => _pending.Modified.Count().ShouldEqual(1);
}

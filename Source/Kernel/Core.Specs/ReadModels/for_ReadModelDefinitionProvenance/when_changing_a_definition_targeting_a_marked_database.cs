// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelDefinitionProvenance;

public class when_changing_a_definition_targeting_a_marked_database : for_ReadModelSinkProvenance.given.a_provenance_inventory
{
    readonly List<Exception?> _errors = [];
    readonly List<ReadModelsManagerState> _failedStates = [];
    ReadModelsManagerStorageProvider _provider;
    ReadModelDefinition _ordinary;
    int _savesBeforeSuccess;
    ReadModelsManagerState _successfulState;

    async Task Because()
    {
        await Marker(EventStoreNamespaceName.Default).MarkQuarantined();
        _markers[(EventStoreNamespaceName.Default, "alias")] = Marker(EventStoreNamespaceName.Default);
        _provider = new(_storage, _provenance);
        _ordinary = _model with { Identifier = "ordinary", Sink = _model.Sink with { Configuration = "clean" } };
        await Attempt(_model with { Owner = ReadModelOwner.Server, ObserverType = ReadModelObserverType.NotSet });
        await Attempt(_model with { Identifier = "alias", Sink = _model.Sink with { Configuration = "alias" } });
        await Attempt(_model with { Sink = _model.Sink with { Configuration = "clean" } });
        await Attempt(_ordinary, _model);
        _savesBeforeSuccess = _eventStore.ReadModels.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "Save");
        _successfulState = new() { ReadModels = [_ordinary], Modified = [_ordinary] };
        await _provider.WriteStateAsync("read-models", GrainId.Create("read-models", Store.Value), new GrainState<ReadModelsManagerState> { State = _successfulState });
    }

    async Task Attempt(params ReadModelDefinition[] definitions)
    {
        var state = new ReadModelsManagerState { ReadModels = definitions, Modified = definitions };
        _failedStates.Add(state);
        _errors.Add(await Catch.Exception(() => _provider.WriteStateAsync("read-models", GrainId.Create("read-models", Store.Value), new GrainState<ReadModelsManagerState> { State = state })));
    }

    [Fact] void should_refuse_relabels_aliases_retargets_and_a_mixed_batch() => _errors.TrueForAll(error => error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_preflight_the_whole_batch_before_any_save() => _savesBeforeSuccess.ShouldEqual(0);
    [Fact] void should_retain_unsaved_modifications_on_failure() => _failedStates.TrueForAll(state => state.Modified.Any()).ShouldBeTrue();
    [Fact] void should_save_an_ordinary_batch() => _eventStore.ReadModels.Received(1).Save(_ordinary);
    [Fact] void should_clear_modifications_only_after_success() => _successfulState.Modified.ShouldBeEmpty();
}

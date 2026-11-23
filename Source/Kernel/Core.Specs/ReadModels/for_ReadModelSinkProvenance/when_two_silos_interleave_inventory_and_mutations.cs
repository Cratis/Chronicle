// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Sinks;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance;

public class when_two_silos_interleave_inventory_and_mutations : given.a_provenance_inventory
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _marked;
    bool _deletedBeforeMark;
    bool _readCompletedBeforeMark;
    bool _deletedAfterMark;
    Exception? _readError;

    void Establish()
    {
        _projection = _projection with { EventSequenceId = "system" };
        _projections = [_projection];
        var marker = Substitute.For<ISinkProvenanceStorage>();
        marker.IsQuarantined().Returns(_ => Task.FromResult(_marked));
        marker.MarkQuarantined().Returns(async _ =>
        {
            _entered.TrySetResult();
            await _release.Task;
            _marked = true;
        });
        _markers[(EventStoreNamespaceName.Default, _model.Sink.Configuration)] = marker;
        _eventStore.Projections.Delete(_projection.Identifier).Returns(_ =>
        {
            _deletedAfterMark = _marked;
            _projections = [];
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        var provider = new ProjectionDefinitionStorageProvider(_storage, TimeProvider.System, _provenance);
        var key = GrainId.Create("projection", new ProjectionKey(_projection.Identifier, Store).ToString());
        var mutation = provider.ClearStateAsync("projection", key, new GrainState<ProjectionDefinition> { State = _projection });
        await _entered.Task;
        _deletedBeforeMark = _eventStore.Projections.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Delete");
        var otherSilo = new ReadModelSinkProvenance(_storage);
        var read = Catch.Exception(() => otherSilo.EnsureReadable(Store, EventStoreNamespaceName.Default, _model));
        _readCompletedBeforeMark = read.IsCompleted;
        _release.TrySetResult();
        await mutation;
        _readError = await read;
    }

    [Fact] void should_not_delete_evidence_before_the_durable_mark() => _deletedBeforeMark.ShouldBeFalse();
    [Fact] void should_not_allow_a_read_while_the_mark_is_pending() => _readCompletedBeforeMark.ShouldBeFalse();
    [Fact] void should_only_delete_after_persistence_completes() => _deletedAfterMark.ShouldBeTrue();
    [Fact] void should_refuse_the_other_silos_read() => ((RpcException)_readError!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}

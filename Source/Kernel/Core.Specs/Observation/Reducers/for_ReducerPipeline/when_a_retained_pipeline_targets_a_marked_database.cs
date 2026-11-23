// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Grpc.Core;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline;

public class when_a_retained_pipeline_targets_a_marked_database : ReadModels.for_ReadModelSinkProvenance.given.a_provenance_inventory
{
    readonly List<Exception?> _errors = [];
    ISink _sink;
    IReadModelsCompliance _compliance;
    int _clientCalls;

    async Task Because()
    {
        _sink = Substitute.For<ISink, ISinkPages>();
        _compliance = Substitute.For<IReadModelsCompliance>();
        var pipeline = new ReducerPipeline(_model, _sink, new ObjectComparer(), _compliance, Store, EventStoreNamespaceName.Default,
            () => _provenance.EnsureReadable(Store, EventStoreNamespaceName.Default, _model));
        var replay = new ReplayContext(new(_model.Identifier, 1), _model.ContainerName, "rebuild", DateTimeOffset.UnixEpoch);
        await pipeline.BeginBulk();
        await pipeline.EndBulk();
        _sink.ClearReceivedCalls();
        await Marker(EventStoreNamespaceName.Default).MarkQuarantined();
        _errors.Add(await Catch.Exception(() => pipeline.Reduce(default!, (_, _) =>
        {
            _clientCalls++;
            throw new InvalidOperationException("Reducer should not be invoked.");
        })));
        _errors.Add(await Catch.Exception(() => pipeline.ReducePage([default!], _ =>
        {
            _clientCalls++;
            throw new InvalidOperationException("Reducer should not be invoked.");
        })));
        _errors.Add(await Catch.Exception(() => pipeline.BeginReplay(replay)));
        _errors.Add(await Catch.Exception(() => pipeline.EndReplay(replay)));
        _errors.Add(await Catch.Exception(() => pipeline.BeginBulk()));
        _errors.Add(await Catch.Exception(() => pipeline.EndBulk()));
    }

    [Fact] void should_refuse_every_retained_operation() => _errors.All(error => error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_exercise_reduction_pages_replay_and_bulk() => _errors.Count.ShouldEqual(6);
    [Fact] void should_not_read_prefetch_or_write_the_sink() => _sink.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_release_initial_state() => _compliance.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_deliver_initial_state_to_the_reducer_client() => _clientCalls.ShouldEqual(0);
}

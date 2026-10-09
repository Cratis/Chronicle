// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_the_request_is_stream_scoped : given.all_dependencies
{
    GetInstanceByKeyResponse _result;
    ImmediateProjectionKey _key;
    IImmediateProjection _projection;
    readonly string _session = Guid.NewGuid().ToString();

    void Establish()
    {
        _projection = Substitute.For<IImmediateProjection>();
        _projection.GetModelInstance().Returns(ProjectionResult.Empty);
        _grainFactory.GetGrain<IImmediateProjection>(Arg.Any<string>()).Returns(call =>
        {
            _key = ImmediateProjectionKey.Parse(call.ArgAt<string>(0));
            return _projection;
        });
    }

    async Task Because()
    {
        _result = await _service.GetInstanceByKey(new()
        {
            EventStore = "store", Namespace = "namespace", ReadModelIdentifier = _readModelDefinition.Identifier,
            EventSequenceId = "event-log", ReadModelKey = "stream-id", SessionId = _session,
            EventSourceId = "source-id", EventSourceType = "source-type", EventStreamType = "stream-type", EventStreamId = "stream-id"
        });
        await _service.DehydrateSession(new()
        {
            EventStore = "store", Namespace = "namespace", ReadModelIdentifier = _readModelDefinition.Identifier,
            EventSequenceId = "event-log", ReadModelKey = "stream-id", SessionId = _session,
            EventSourceId = "source-id", EventSourceType = "source-type", EventStreamType = "stream-type", EventStreamId = "stream-id"
        });
    }

    [Fact] void should_echo_scope_even_for_an_absent_model() => _result.StreamScoped.ShouldBeTrue();
    [Fact] void should_keep_the_model_key_separate_from_the_source() => _key.ReadModelKey.Value.ShouldEqual("stream-id");
    [Fact] void should_use_the_source_in_the_stream_scope() => _key.StreamScope!.EventSourceId.Value.ShouldEqual("source-id");
    [Fact] void should_use_the_stream_id_in_the_stream_scope() => _key.StreamScope!.EventStreamId.Value.ShouldEqual("stream-id");
    [Fact] void should_dehydrate_the_folded_session() => _projection.Received(1).Dehydrate();
}

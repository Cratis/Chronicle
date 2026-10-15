// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_a_stream_scoped_request_has_no_session : given.all_dependencies
{
    GetInstanceByKeyResponse _result;
    ImmediateProjectionKey _key;
    IImmediateProjection _projection;

    void Establish()
    {
        _readModelDefinition = _readModelDefinition with
        {
            Sink = new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.InMemory)
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);
        _projection = Substitute.For<IImmediateProjection>();
        _projection.GetModelInstance().Returns(ProjectionResult.Empty);
        _grainFactory.GetGrain<IImmediateProjection>(Arg.Any<string>()).Returns(call =>
        {
            _key = ImmediateProjectionKey.Parse(call.ArgAt<string>(0));
            return _projection;
        });
    }

    async Task Because() => _result = await _service.GetInstanceByKey(new()
    {
        EventStore = "store", Namespace = "namespace", ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log", ReadModelKey = "stream-id",
        EventSourceId = "source-id", EventStreamType = "stream-type", EventStreamId = "stream-id"
    });

    [Fact] void should_fold_the_projection() => _projection.Received(1).GetModelInstance();
    [Fact] void should_not_read_materialized_state() => _sink.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_echo_the_stream_scope() => _result.StreamScoped.ShouldBeTrue();
    [Fact] void should_keep_the_stream_scope_on_the_projection_key() => _key.StreamScope!.EventStreamType.Value.ShouldEqual("stream-type");
    [Fact] void should_keep_the_event_source_on_the_projection_key() => _key.StreamScope!.EventSourceId.Value.ShouldEqual("source-id");
}

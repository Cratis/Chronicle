// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections;
using Grpc.Core;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_the_stream_type_is_all : given.all_dependencies
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _service.GetInstanceByKey(new()
    {
        EventStore = "store", Namespace = "namespace", ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log", ReadModelKey = "source", EventStreamType = EventStreamType.All.Value, EventStreamId = "stream-id"
    }));

    [Fact] void should_fail_the_request() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.FailedPrecondition);
    [Fact] void should_not_fold_an_unscoped_projection() => _grainFactory.DidNotReceive().GetGrain<IImmediateProjection>(Arg.Any<string>());
    [Fact] void should_not_read_materialized_state() => _sink.ReceivedCalls().ShouldBeEmpty();
}

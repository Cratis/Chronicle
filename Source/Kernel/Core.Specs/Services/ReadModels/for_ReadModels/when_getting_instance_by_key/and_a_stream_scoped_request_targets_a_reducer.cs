// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Grpc.Core;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_a_stream_scoped_request_targets_a_reducer : given.all_dependencies
{
    Exception _error;

    void Establish() => _readModel.GetDefinition().Returns(_readModelDefinition with { ObserverType = ReadModelObserverType.Reducer });

    async Task Because() => _error = await Catch.Exception(() => _service.GetInstanceByKey(new()
    {
        EventStore = "store", Namespace = "namespace", ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log", ReadModelKey = "source", EventStreamType = "stream-type", EventStreamId = "stream-id"
    }));

    [Fact] void should_fail_the_request() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.FailedPrecondition);
    [Fact] void should_not_call_a_connected_reducer() => _reducerMediator.ReceivedCalls().ShouldBeEmpty();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Grpc.Core;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_tagged_rpc_is_unimplemented : given.a_unit_of_work
{
    Exception _error;

    void Establish()
    {
        _unitOfWork.AddEventWithNamedTags(EventSequenceId.Log, EventSourceId.New(), "event", [new("name", "value")], Causation.Unknown());
        _eventSequence.AppendManyWithNamedTags(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<IEnumerable<NamedTag>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns<Task<AppendManyResult>>(_ => throw new RpcException(new Status(StatusCode.Unimplemented, "No tagged RPC")));
    }

    async Task Because() => _error = await Catch.Exception(_unitOfWork.Commit);

    [Fact] void should_propagate_rpc_failure() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_not_retry_plain_append() => _eventSequence.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
    [Fact] void should_complete_the_unit_even_on_failure() => _onCompletedCalled.ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_stream_acquisition : given.a_protected_reader
{
    DecisionRead<Model> _read;
    Exception _aggregateError;

    void Establish()
    {
        _definition.From.First().Value.Key = "$eventContext(eventStreamId)";
        _readModels.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>(), Arg.Any<CallContext>()).Returns(new GetInstanceByKeyResponse
        {
            StreamScoped = true, LastHandledEventSequenceNumber = 4, ReadModel = "{\"id\":\"stream-id\"}"
        });
    }

    async Task Because()
    {
        using var frame = _unit.EnforceReadProtection();
        _read = await _reader.GetFor<Model>(_unit, "source", "stream-type", "stream-id");
        _aggregateError = Catch.Exception(() => _unit.EnrolContributedRead(new(ConcurrencyScopeOrigin.Aggregate, "source", new ConcurrencyScope(4, EventSourceId: "source"), _store.Name, _store.Namespace, EventSequenceId.Log, "Aggregate")));
    }

    [Fact] void should_return_the_stream_key() => _read.Key.Value.ShouldEqual("stream-id");
    [Fact] void should_enroll_the_source_before_returning() => _aggregateError.ShouldBeOfExactType<ReadAndAggregateOnSameSource>();
}

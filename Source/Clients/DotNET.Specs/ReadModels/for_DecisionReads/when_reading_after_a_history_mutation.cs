// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads;

public class when_reading_after_a_history_mutation : given.a_decision_reader
{
    DecisionRead<Model> _read;

    void Establish() => _sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>(), Arg.Any<CallContext>())
        .Returns(call => QueryResult<EventSequenceTailResponse>.Success(Guid.NewGuid(),
            new() { SequenceNumber = call.Arg<TailSequenceNumberRequest>().EventSourceId is null ? _boundary : _probe, MutationEpoch = call.Arg<TailSequenceNumberRequest>().IncludeMutationEpoch ? 6UL : null }));

    async Task Because() => _read = await _reader.GetDetached<Model>("source");

    [Fact] void should_carry_the_epoch_on_the_scope() => ((IDecisionRead)_read).Scope.MutationEpoch.ShouldEqual(6UL);
    [Fact] void should_ask_for_the_epoch_only_on_the_unfiltered_boundary_request() => _sequences.Received(1).TailSequenceNumber(Arg.Is<TailSequenceNumberRequest>(_ => _.IncludeMutationEpoch), Arg.Any<CallContext>());
}

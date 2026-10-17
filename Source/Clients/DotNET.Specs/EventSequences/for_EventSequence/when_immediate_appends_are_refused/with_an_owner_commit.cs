// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_an_owner_commit : given.a_unit_with_native_sequence
{
    Exception _secondCommitError;
    Exception _lateAppendError;

    async Task Because()
    {
        using var frame = Refuse();
        await _sequence.Transactional.Append("source", "changed");
        ClearCalls();
        await frame.Commit();
        _secondCommitError = await Catch.Exception(frame.Commit);
        _lateAppendError = await Catch.Exception(() => _sequence.Append("source", "too late"));
    }

    [Fact] void should_commit_the_enrolled_event() => _request.Events.Count().ShouldEqual(1);
    [Fact] void should_commit_to_the_original_source() => _request.Events.Single().EventSourceId.ShouldEqual("source");
    [Fact] async Task should_commit_exactly_once() => await _sequences.Received(1).AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), Arg.Any<CallContext>());
    [Fact] void should_mark_the_unit_successful() => _unit.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_allow_a_second_commit() => _secondCommitError.ShouldBeOfExactType<UnitOfWorkIsAlreadyCommitted>();
    [Fact] void should_keep_refusal_after_completion() => _lateAppendError.ShouldBeOfExactType<ImmediateAppendRefused>();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_enrollment_during_an_in_flight_commit : given.a_trusted_commit
{
    Exception _protectedError;
    Exception _contributedError;
    Exception _stagingError;
    bool _wasInFlight;

    Task Because() => Execute(async () =>
    {
        var response = new TaskCompletionSource<CommandResult<AppendManyResponse>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _sequences.AppendManyForEventSources(Arg.Any<AppendManyForEventSourcesRequest>(), Arg.Any<CallContext>()).Returns(response.Task);
        Read();
        _unit.AddEvents(EventSequenceId.Log, [new("source", "planned")], []);
        var commit = Commit();
        _wasInFlight = !commit.IsCompleted;
        _protectedError = Catch.Exception(() => _unit.AddDecisionRead(new DecisionRead<object>("other", null, "store", "tenant", 100, [_changed])));
        _contributedError = Catch.Exception(EnrollReadScope);
        _stagingError = Catch.Exception(() => _unit.AddEvents(EventSequenceId.Log, [new("source", "late")], []));
        response.SetResult(CommandResult<AppendManyResponse>.Success(Guid.NewGuid(), new() { SequenceNumbers = [101], ConstraintViolations = [], ConcurrencyViolations = [], Errors = [] }));
        await commit;
    });

    [Fact] void should_exercise_an_unfinished_append() => _wasInFlight.ShouldBeTrue();
    [Fact] void should_refuse_a_protected_read_after_the_snapshot() => _protectedError.ShouldBeOfExactType<ReadAfterProtectedCommit>();
    [Fact] void should_refuse_a_contributed_read_after_the_snapshot() => _contributedError.ShouldBeOfExactType<ReadAfterProtectedCommit>();
    [Fact] void should_refuse_late_staging() => _stagingError.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
    [Fact] void should_finish_the_original_commit() => _error.ShouldBeNull();
    [Fact] void should_keep_only_the_original_event() => _unit.GetEvents().ShouldContainOnly("planned");
}

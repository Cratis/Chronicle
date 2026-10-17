// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_existing_read_protection : given.a_unit_with_native_sequence
{
    Exception _readError;
    Exception _afterRefusalError;
    Exception _afterRefusalReadError;
    Exception _frameCommitError;
    Exception _frameRollbackError;
    EventSequenceNumber _allowedTail;

    async Task Because()
    {
        using var protection = _unit.EnforceReadProtection();
        using (var owner = Refuse())
        {
            _readError = await Catch.Exception(() => _sequence.GetTailSequenceNumber());
            using (var region = _unit.AllowReads(ReadAllowance.Advisory, "display only"))
            {
                _allowedTail = await _sequence.GetTailSequenceNumber();
                _error = await Catch.Exception(() => _sequence.Append("source", "escaped"));
            }
            _unit.AddDecisionRead(new DecisionRead<object>("dependency", null, "store", "tenant", 7, [new EventType("changed", EventTypeGeneration.First)]));
            await _sequence.Transactional.Append("source", "changed", concurrencyScope: ConcurrencyScope.None);
            _frameCommitError = await Catch.Exception(owner.Commit);
            _frameRollbackError = await Catch.Exception(owner.Rollback);
            var completionOwner = _unit.ClaimDecisionReadCommitOwnership();
            await _unit.CommitAsOwner(completionOwner);
        }
        _afterRefusalError = await Catch.Exception(() => _sequence.Append("source", "still protected"));
        _afterRefusalReadError = await Catch.Exception(() => _sequence.GetTailSequenceNumber());
    }

    [Fact] void should_keep_the_existing_read_refusal() => _readError.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
    [Fact] void should_keep_read_allowances_working() => _allowedTail.ShouldEqual((EventSequenceNumber)40);
    [Fact] void should_not_let_a_read_allowance_admit_an_append() => _error.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_not_give_completion_to_the_refusal_frame() => _frameCommitError.ShouldBeOfExactType<ImmediateAppendRefusalRequiresOwner>();
    [Fact] void should_not_give_rollback_to_the_refusal_frame() => _frameRollbackError.ShouldBeOfExactType<ImmediateAppendRefusalRequiresOwner>();
    [Fact] void should_allow_the_read_protection_owner_to_commit() => _unit.IsSuccess.ShouldBeTrue();
    [Fact] void should_use_the_protected_commit_path() => _request.ConcurrencyScopes.ShouldNotBeEmpty();
    [Fact] void should_preserve_read_protection_after_refusal_disposal() => _afterRefusalError.ShouldBeOfExactType<ImmediateAppendWhileReadsAreProtected>();
    [Fact] void should_preserve_read_admission_after_refusal_disposal() => _afterRefusalReadError.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
}

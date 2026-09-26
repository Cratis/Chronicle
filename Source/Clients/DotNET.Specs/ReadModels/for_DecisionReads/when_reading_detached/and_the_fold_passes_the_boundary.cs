// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_the_fold_passes_the_boundary : given.a_decision_reader
{
    DecisionRead<Model> _read;
    UnitOfWork _unit;

    void Establish()
    {
        _last = 6;
        _probe = 6;
        var sequence = Substitute.For<IEventSequence>();
        _store.GetEventSequence(EventSequenceId.Log).Returns(sequence);
        sequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns(call => new AppendManyResult
            {
                ConcurrencyViolations = [new ConcurrencyViolation("source", 5, 6)]
            });
        _unit = new UnitOfWork(CorrelationId.New(), _ => { }, _store);
    }

    async Task Because()
    {
        _read = await _reader.GetDetached<Model>("source");
        _unit.AddDecisionRead(_read);
        await _unit.CommitAsOwner(_unit.ClaimDecisionReadCommitOwnership());
    }

    [Fact] void should_retry_twice() => _folds.ShouldEqual(3);
    [Fact] void should_keep_the_pre_fold_boundary() => ((IDecisionRead)_read).Scope.SequenceNumber.ShouldEqual((EventSequenceNumber)5);
    [Fact] void should_report_a_conflict_after_retry() => _unit.GetDecisionConflicts().ShouldContain(new DecisionConflict(typeof(Model), "source"));
}

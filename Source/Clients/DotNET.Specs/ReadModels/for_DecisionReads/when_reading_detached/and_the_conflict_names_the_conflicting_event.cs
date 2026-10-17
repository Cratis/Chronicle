// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_the_conflict_names_the_conflicting_event : given.a_decision_reader
{
    UnitOfWork _unit;
    DecisionConflict _conflict;

    void Establish()
    {
        var sequence = Substitute.For<IEventSequence>();
        _store.GetEventSequence(EventSequenceId.Log).Returns(sequence);
        sequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns(call => new AppendManyResult
            {
                ConcurrencyViolations = [new ConcurrencyViolation("source", 4, 7) { ConflictingEventTypeId = "created", ConflictingEventSequenceNumber = 5 }]
            });
        _unit = new UnitOfWork(CorrelationId.New(), _ => { }, _store);
    }

    async Task Because()
    {
        _unit.AddDecisionRead(await _reader.GetDetached<Model>("source"));
        await _unit.CommitAsOwner(_unit.ClaimDecisionReadCommitOwnership());
        _conflict = _unit.GetDecisionConflicts().Single();
    }

    [Fact] void should_report_the_read_model_and_key() => (_conflict.ReadModelType, _conflict.Key).ShouldEqual((typeof(Model), (ReadModelKey)"source"));
    [Fact] void should_name_the_conflicting_event_type() => _conflict.ConflictingEventTypeId.ShouldEqual(new EventTypeId("created"));
    [Fact] void should_name_the_conflicting_event_sequence_number() => _conflict.ConflictingEventSequenceNumber.ShouldEqual(new EventSequenceNumber(5));
}

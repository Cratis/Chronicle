// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_multiple_scopes;

public class and_one_scope_has_a_changed_mutation_epoch : given.a_concurrency_validator
{
    ConcurrencyScopes _scopes;
    IEnumerable<ConcurrencyViolation> _result;
    EventSourceId _stale;
    EventSourceId _current;

    void Establish()
    {
        _stale = EventSourceId.New();
        _current = EventSourceId.New();
        _scopes = new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>
        {
            { _stale, new ConcurrencyScope(10, true, null, null, null, null) { MutationEpoch = new EventSequenceMutationEpoch(2) } },
            { _current, new ConcurrencyScope(10, true, null, null, null, null) { MutationEpoch = new EventSequenceMutationEpoch(4) } }
        });
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>()).Returns(10UL);
    }

    async Task Because() => _result = await _validator.Validate(_scopes);

    [Fact] void should_report_one_violation() => _result.Count().ShouldEqual(1);
    [Fact] void should_report_it_for_the_stale_source() => _result.Single().EventSourceId.ShouldEqual(_stale);
}

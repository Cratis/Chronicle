// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

public class and_the_scope_carries_no_mutation_epoch : given.a_concurrency_validator
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _scope;
    Option<ConcurrencyViolation> _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scope = new ConcurrencyScope(10, true, null, null, null, null) { MutationEpoch = null };
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _eventSourceId, Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>()).Returns(10UL);
    }

    async Task Because() => _result = await _validator.Validate(_eventSourceId, _scope);

    [Fact] void should_not_report_a_violation() => _result.HasValue.ShouldBeFalse();
}

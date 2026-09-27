// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

public class and_decision_read_has_a_later_matching_append : given.a_concurrency_validator
{
    readonly EventSourceId _source = "source";
    readonly EventType[] _types = [new("Created", EventTypeGeneration.First), new("Removed", EventTypeGeneration.First)];
    ConcurrencyScope _scope;
    Option<ConcurrencyViolation> _result;

    void Establish()
    {
        _scope = new ConcurrencyScope(7, true, null, null, null, _types);
        _eventSequenceStorage.GetTailSequenceNumber(_types, _source, null, null, null).Returns((EventSequenceNumber)8);
    }

    async Task Because() => _result = await _validator.Validate(_source, _scope);

    [Fact] void should_reject_the_stale_decision() => _result.HasValue.ShouldBeTrue();
    [Fact] void should_query_only_the_read_source_and_projected_event_types() =>
        _eventSequenceStorage.Received(1).GetTailSequenceNumber(_types, _source, null, null, null);
}

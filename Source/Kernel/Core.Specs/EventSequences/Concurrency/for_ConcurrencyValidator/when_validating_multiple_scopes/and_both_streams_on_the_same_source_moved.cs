// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_multiple_scopes;

public class and_both_streams_on_the_same_source_moved : given.a_concurrency_validator
{
    ConcurrencyViolation[] _violations;

    void Establish() => _eventSequenceStorage.GetTailSequenceNumber(null, (EventSourceId)"source", null, Arg.Any<EventStreamId>(), null).Returns((EventSequenceNumber)6UL);

    async Task Because() => _violations = (await _validator.Validate(new ConcurrencyScopeConjunctions([
        ("source", new(5, true, null, "first", null, null), 0),
        ("source", new(5, true, null, "second", null, null), 1)
    ]))).ToArray();

    [Fact] void should_report_every_failed_guard() => _violations.Select(_ => _.ScopeIndex).ShouldContainOnly((uint?)0, (uint?)1);
}

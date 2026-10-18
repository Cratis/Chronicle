// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_multiple_scopes;

public class and_one_of_two_streams_on_the_same_source_moved : given.a_concurrency_validator
{
    ConcurrencyScopeConjunctions _scopes;
    ConcurrencyViolation[] _violations;

    void Establish()
    {
        _scopes = new([("source", new(5, true, "stream", "first", null, null), 0), ("source", new(5, true, "stream", "second", null, null), 2)]);
        _eventSequenceStorage.GetTailSequenceNumber(null, (EventSourceId)"source", null, (EventStreamId)"first", (EventStreamType)"stream").Returns((EventSequenceNumber)5UL);
        _eventSequenceStorage.GetTailSequenceNumber(null, (EventSourceId)"source", null, (EventStreamId)"second", (EventStreamType)"stream").Returns((EventSequenceNumber)6UL);
    }

    async Task Because() => _violations = (await _validator.Validate(_scopes)).ToArray();

    [Fact] void should_refuse_only_the_moved_stream() => _violations.Length.ShouldEqual(1);
    [Fact] void should_report_the_original_request_position() => _violations.Single().ScopeIndex.ShouldEqual((uint?)2);
    [Fact] void should_echo_the_failed_predicate() => _violations.Single().Scope!.EventStreamId.ShouldEqual("second");
}

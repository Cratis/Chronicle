// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_ConflictingEnrollmentScope.when_describing_a_conflict;

public class with_sensitive_scope_values : Specification
{
    EventSourceId _source;
    EventSequenceId _sequence;
    ConcurrencyScope _effective;
    ConcurrencyScope _required;
    ConflictingEnrollmentScope _conflict;
    string[] _sensitiveValues;

    void Establish()
    {
        _source = "sensitive-enrolled-source";
        _sequence = "sensitive-sequence";
        _effective = new(13579, "sensitive-effective-source", "sensitive-effective-stream-type", "sensitive-effective-stream-id", "sensitive-effective-source-type", [new("sensitive-effective-event", EventTypeGeneration.First)]);
        _required = new(24680, "sensitive-required-source", "sensitive-required-stream-type", "sensitive-required-stream-id", "sensitive-required-source-type", [new("sensitive-required-event", EventTypeGeneration.First)]);
        _sensitiveValues =
        [
            _source.Value, _sequence.Value,
            _effective.SequenceNumber.ToString(), _required.SequenceNumber.ToString(),
            _effective.EventSourceId!.Value, _required.EventSourceId!.Value,
            _effective.EventSourceType!.Value, _required.EventSourceType!.Value,
            _effective.EventStreamType!.Value, _required.EventStreamType!.Value,
            _effective.EventStreamId!.Value, _required.EventStreamId!.Value,
            _effective.EventTypes!.Single().Id.Value, _required.EventTypes!.Single().Id.Value
        ];
    }

    void Because() => _conflict = new(_source, _effective, _required, _sequence);

    [Fact] void should_describe_the_differing_dimensions_without_values() => _conflict.Message.ShouldEqual("Enrollment conflicts with its required scope or event sequence. Differing dimensions: sequence number, event source id, event source type, event stream type, event stream id, event types.");
    [Fact] void should_redact_each_sensitive_value() => _sensitiveValues.ShouldEachConformTo(value => !_conflict.Message.Contains(value, StringComparison.Ordinal));
    [Fact] void should_not_format_the_effective_scope() => _conflict.Message.ShouldNotContain(_effective.ToString());
    [Fact] void should_not_format_the_required_scope() => _conflict.Message.ShouldNotContain(_required.ToString());
    [Fact] void should_preserve_the_enrolled_source() => _conflict.EventSourceId.ShouldEqual(_source);
    [Fact] void should_preserve_the_attempted_sequence() => _conflict.EventSequenceId.ShouldEqual(_sequence);
    [Fact] void should_preserve_the_effective_scope() => _conflict.Effective.ShouldBeSame(_effective);
    [Fact] void should_preserve_the_required_scope() => _conflict.Required.ShouldBeSame(_required);
}

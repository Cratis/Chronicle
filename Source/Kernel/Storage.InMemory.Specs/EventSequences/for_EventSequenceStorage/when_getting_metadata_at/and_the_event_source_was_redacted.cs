// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_getting_metadata_at;

public class and_the_event_source_was_redacted : given.an_event_for_redacted_metadata
{
    async Task Establish() => await _storage.Redact(_entry.EventSourceId, "reason", [_entry.EventType], _redactionCorrelation, [_redactionCausation], [_redactor], _redactionOccurred);

    async Task Because() => await Read();

    [Fact] void should_return_the_redaction_type_like_persistent_providers() => _result.EventTypeId.ShouldEqual(GlobalEventTypes.Redaction);
    [Fact] void should_return_the_redaction_time() => _result.Occurred.ShouldEqual(_redactionOccurred);
    [Fact] void should_return_the_redaction_correlation() => _result.CorrelationId.ShouldEqual(_redactionCorrelation);
    [Fact] void should_return_the_redaction_causation() => _result.Causation.Single().Type.ShouldEqual(_redactionCausation.Type);
    [Fact] void should_return_the_redaction_causation_time() => _result.Causation.Single().Occurred.ShouldEqual(_redactionCausation.Occurred);
    [Fact] void should_return_the_redactor_chain() => _result.CausedByChain.ShouldContainOnly(_redactor);
    [Fact] void should_preserve_routing() => _result.EventStreamId.ShouldEqual(_entry.EventStreamId);
    [Fact] void should_preserve_tags() => _result.Tags.ShouldContainOnly(_entry.Tags);
    [Fact] void should_preserve_subject() => _result.Subject.ShouldEqual(_entry.Subject);
    [Fact] void should_not_resolve_or_create_identities_during_reads() => _identities.ReceivedCalls().ShouldBeEmpty();
}

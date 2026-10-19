// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_it_equals_a_publication_identity : given.an_event_sequence_with_idempotency_keys
{
    void Establish() => _durable["one"] = new(EventSequenceNumber.First, "publication-fingerprint");
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_keep_the_identity_domains_separate() => _result.Outcome.ShouldEqual(AppendOutcome.Committed);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_write_the_prefixed_identity() => _written.Single().Identity!.Id.ShouldEqual("append-key:one");
}

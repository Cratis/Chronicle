// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSources.when_handling;

public class and_a_receipted_batch_has_stream_conjunctions : given.an_append_endpoint
{
    readonly Occurrences.EffectReceipt _receipt = new("occurrence", "effect", 1);
    AppendManyForEventSources _command;
    ConcurrencyScopeConjunctions _forwarded;

    void Establish()
    {
        _command = new(
            "store",
            "namespace",
            EventSequenceId.Log,
            [],
            ConcurrencyScopes:
            [
                new("source", new(5, true, "stream", "first")),
                new("source", new(5, true, "stream", "second"))
            ],
            EffectReceipt: _receipt);
        _eventSequence.AppendManyWithConjunctionsAndEffectReceipt(Arg.Any<IEnumerable<EventSequences.EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(), Arg.Any<Concepts.Identities.Identity>(), Arg.Any<ConcurrencyScopeConjunctions>(), _receipt)
            .Returns(call =>
            {
                _forwarded = call.Arg<ConcurrencyScopeConjunctions>();
                return Task.FromResult(new AppendManyResult());
            });
    }

    async Task Because() => await _command.Handle(_grainFactory, _causation, _principal);

    [Fact] void should_forward_every_guard_with_the_receipt() => _forwarded.Entries.Count.ShouldEqual(2);
    [Fact] void should_not_use_the_legacy_receipt_method() => _eventSequence.DidNotReceive().AppendManyWithEffectReceipt(Arg.Any<IEnumerable<EventSequences.EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(), Arg.Any<Concepts.Identities.Identity>(), Arg.Any<ConcurrencyScopes>(), Arg.Any<Occurrences.EffectReceipt>());
}

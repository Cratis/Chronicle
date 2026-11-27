// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using StoredEffectReceipt = Cratis.Chronicle.Storage.Occurrences.EffectReceipt;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_replaying_an_effect_with_conjunctions : given.a_namespace_bound_receipt_sequence
{
    AppendManyResult _first = null!;
    AppendManyResult _replay = null!;

    async Task Because()
    {
        _first = await _eventSequence.AppendManyWithEffectReceiptToNamespace(Incarnation, _events, _correlationId, [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()), _receipt);
        _replay = await _eventSequence.AppendManyWithConjunctionsAndEffectReceiptToNamespace(Incarnation, _events, _correlationId, [], Identity.System, Conjunctions, _receipt);
    }

    [Fact] void should_commit_every_event_atomically_with_the_receipt() => _first.SequenceNumbers.Count().ShouldEqual(_events.Count);
    [Fact] void should_report_the_replay() => _replay.EffectAlreadyRecorded.ShouldBeTrue();
    [Fact] void should_write_events_and_receipt_once() => _fenced.Received(1).AppendManyWithEffectReceipt(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<EventToAppendToStorage>>(), Arg.Any<StoredEffectReceipt>());
    [Fact] void should_keep_the_expected_identity() => _receiptContexts.TrueForAll(context => context.Incarnation == Incarnation && context.ActivationIncarnation == Incarnation).ShouldBeTrue();
    [Fact] void should_not_use_tokenless_appends() => TokenlessAppendCount.ShouldEqual(0);
}

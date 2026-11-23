// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Occurrences;
using StoredEffectReceipt = Cratis.Chronicle.Storage.Occurrences.EffectReceipt;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_recording_a_receipt_without_events : given.a_namespace_bound_receipt_sequence
{
    RecordEffectReceiptResult _first = null!;
    RecordEffectReceiptResult _replay = null!;

    async Task Because()
    {
        _first = await Record();
        _replay = await Record();
    }

    Task<RecordEffectReceiptResult> Record() => _eventSequence.RecordEffectReceiptToNamespace(Incarnation, _receipt, EffectOutcome.NoEvents, []);

    [Fact] void should_record_the_first_receipt() => _first.Status.ShouldEqual(EffectReceiptStatus.Recorded);
    [Fact] void should_replay_the_existing_receipt() => _replay.Status.ShouldEqual(EffectReceiptStatus.AlreadyRecorded);
    [Fact] void should_only_write_once() => _fenced.Received(1).AppendManyWithEffectReceipt(Arg.Any<NamespaceCommitContext>(), Arg.Is<IEnumerable<EventToAppendToStorage>>(events => !events.Any()), Arg.Any<StoredEffectReceipt>());
    [Fact] void should_fence_the_replay() => _fenced.Received(2).GetEffectReceipt(Arg.Any<NamespaceCommitContext>(), Arg.Any<string>());
    [Fact] void should_retain_the_expected_identity() => _receiptContexts.TrueForAll(context => context.Incarnation == Incarnation && context.ActivationIncarnation == Incarnation).ShouldBeTrue();
}

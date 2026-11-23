// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Occurrences;
using NSubstitute.Extensions;
using StoredEffectReceipt = Cratis.Chronicle.Storage.Occurrences.EffectReceipt;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_a_receipt_write_has_unknown_outcome : given.a_namespace_bound_receipt_sequence
{
    readonly NamespaceCommitOutcomeUnknown _failure = new(new TimeoutException());
    Exception? _error;

    void Establish() => _fenced.Configure().AppendManyWithEffectReceipt(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<EventToAppendToStorage>>(), Arg.Any<StoredEffectReceipt>())
        .Returns(Task.FromException<EffectReceiptWriteResult>(_failure));

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventSequence.RecordEffectReceiptToNamespace(Incarnation, _receipt, EffectOutcome.NoEvents, []));

    [Fact] void should_preserve_the_unknown_commit() => _error.ShouldEqual(_failure);
    [Fact] void should_not_probe_again_or_relabel_unknown_as_refused() => _fenced.Received(1).GetEffectReceipt(Arg.Any<NamespaceCommitContext>(), Arg.Any<string>());
    [Fact] void should_not_retry_the_write() => _fenced.Received(1).AppendManyWithEffectReceipt(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<EventToAppendToStorage>>(), Arg.Any<StoredEffectReceipt>());
}

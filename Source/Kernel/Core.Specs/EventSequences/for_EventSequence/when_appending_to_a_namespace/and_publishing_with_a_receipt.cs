// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_publishing_with_a_receipt : given.a_namespace_bound_receipt_sequence
{
    AppendResult _first = null!;
    AppendResult _replay = null!;

    async Task Because()
    {
        _first = await Publish();
        _replay = await Publish();
    }

    Task<AppendResult> Publish() => _eventSequence.AppendPublicationToNamespace(Incarnation, "publication", "fingerprint", _events[0], _correlationId, [], Identity.System);

    [Fact] void should_commit_the_publication() => _first.IsSuccess.ShouldBeTrue();
    [Fact] void should_replay_the_durable_slot() => _replay.SequenceNumber.ShouldEqual(_first.SequenceNumber);
    [Fact] void should_write_the_receipt_once() => _fenced.Received(1).AppendPublication(Arg.Any<NamespaceCommitContext>(), Arg.Any<EventPublication>(), Arg.Any<EventToAppendToStorage>());
    [Fact] void should_not_substitute_an_ordinary_fenced_append() => _fenced.DidNotReceive().Append(Arg.Any<NamespaceCommitContext>(), Arg.Any<EventToAppendToStorage>());
    [Fact] void should_fence_both_receipt_lookups() => _fenced.Received(2).TryGetPublication(Arg.Any<NamespaceCommitContext>(), Arg.Any<EventPublication>());
    [Fact] void should_retain_the_expected_identity_everywhere() => _receiptContexts.TrueForAll(context => context.Incarnation == Incarnation && context.ActivationIncarnation == Incarnation).ShouldBeTrue();
    [Fact] void should_not_use_tokenless_appends() => TokenlessAppendCount.ShouldEqual(0);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_replaying_an_idempotency_key : given.a_namespace_bound_receipt_sequence
{
    IdempotentAppendResult _first = null!;
    IdempotentAppendResult _replay = null!;

    async Task Because()
    {
        _first = await Append();
        _replay = await Append();
    }

    Task<IdempotentAppendResult> Append() => _eventSequence.AppendWithIdempotencyKeyToNamespace(Incarnation, _events[0], _key, _correlationId, [], Identity.System, ConcurrencyScope.None);

    [Fact] void should_commit_the_first_intent() => _first.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_the_replay() => _replay.Outcome.ShouldEqual(AppendOutcome.AlreadyAppended);
    [Fact] void should_preserve_the_durable_slot() => _replay.Result.SequenceNumber.ShouldEqual(_first.Result.SequenceNumber);
    [Fact] void should_only_write_once() => _fenced.Received(1).AppendManyWithIdentities(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<EventToAppendToStorage>>());
    [Fact] void should_fence_both_key_lookups() => _fenced.Received(2).TryGetPublications(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<string>>());
    [Fact] void should_not_use_tokenless_appends() => TokenlessAppendCount.ShouldEqual(0);
}

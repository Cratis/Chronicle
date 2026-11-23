// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_appending_a_mixed_keyed_batch : given.a_namespace_bound_receipt_sequence
{
    IdempotentAppendManyResult _result = null!;

    async Task Because() => _result = await _eventSequence.AppendManyWithIdempotencyKeysToNamespace(Incarnation, _events, [_key, null, null], _correlationId, [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_confirm_every_event() => _result.Result.SequenceNumbers.Count().ShouldEqual(_events.Count);
    [Fact] void should_use_one_atomic_fenced_append() => _fenced.Received(1).AppendManyWithIdentities(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<EventToAppendToStorage>>());
    [Fact] void should_fence_the_key_lookup() => _fenced.Received(1).TryGetPublications(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<string>>());
    [Fact] void should_not_use_tokenless_appends() => TokenlessAppendCount.ShouldEqual(0);
}

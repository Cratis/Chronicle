// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_an_empty_keyed_batch_is_rejected : given.a_namespace_bound_receipt_sequence
{
    readonly NamespaceCommitRejected _failure = new();
    Exception? _error;

    void Establish() => _fenced.Configure().TryGetPublications(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<string>>())
        .Returns(Task.FromException<IReadOnlyDictionary<string, StoredEventPublication>>(_failure));

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventSequence.AppendManyWithIdempotencyKeysToNamespace(Incarnation, [], [], _correlationId, [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>())));

    [Fact] void should_not_return_empty_success_without_a_fence() => _error.ShouldEqual(_failure);
    [Fact] void should_attempt_the_fence_once() => _fenced.Received(1).TryGetPublications(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<string>>());
    [Fact] void should_not_append_events() => _fenced.DidNotReceive().AppendManyWithIdentities(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<EventToAppendToStorage>>());
}

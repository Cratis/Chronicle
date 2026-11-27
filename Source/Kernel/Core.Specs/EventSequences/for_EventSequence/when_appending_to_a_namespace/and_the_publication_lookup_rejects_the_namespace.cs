// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Monads;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_the_publication_lookup_rejects_the_namespace : given.a_namespace_bound_receipt_sequence
{
    readonly NamespaceCommitRejected _failure = new();
    Exception? _error;

    void Establish() => _fenced.Configure().TryGetPublication(Arg.Any<NamespaceCommitContext>(), Arg.Any<EventPublication>())
        .Returns(Task.FromException<Option<EventPublicationReceipt>>(_failure));

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventSequence.AppendPublicationToNamespace(Incarnation, "publication", "fingerprint", _events[0], _correlationId, [], Identity.System));

    [Fact] void should_propagate_the_fence_refusal() => _error.ShouldEqual(_failure);
    [Fact] void should_not_append_an_event_or_receipt() => _fenced.DidNotReceive().AppendPublication(Arg.Any<NamespaceCommitContext>(), Arg.Any<EventPublication>(), Arg.Any<EventToAppendToStorage>());
    [Fact] void should_not_use_tokenless_appends() => TokenlessAppendCount.ShouldEqual(0);
}

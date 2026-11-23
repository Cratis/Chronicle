// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Monads;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_the_activation_must_be_reloaded : given.a_namespace_bound_sequence
{
    readonly NamespaceIncarnationRequiresReactivation _failure = new();
    Exception? _error;

    void Establish() => _fenced.Configure().Append(Arg.Any<NamespaceCommitContext>(), Arg.Any<EventToAppendToStorage>()).Returns(Task.FromException<Result<AppendedEvent, DuplicateEventSequenceNumber>>(_failure));

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(AppendProtected);

    [Fact] void should_return_the_reactivation_signal_without_normalizing_it() => _error.ShouldEqual(_failure);
    [Fact] void should_not_retry_with_stale_caches() => _fenced.Received(1).Append(Arg.Any<NamespaceCommitContext>(), Arg.Any<EventToAppendToStorage>());
    [Fact] void should_not_use_tokenless_storage() => TokenlessAppendCount.ShouldEqual(0);
}

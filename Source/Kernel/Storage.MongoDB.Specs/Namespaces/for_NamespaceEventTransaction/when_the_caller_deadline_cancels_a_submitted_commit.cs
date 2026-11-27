// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_caller_deadline_cancels_a_submitted_commit : given.a_namespace_transaction
{
    void Establish()
    {
        _context = _context with { Deadline = _clock.GetUtcNow() + TimeSpan.FromSeconds(1) };
        _duringCommit = () =>
        {
            _clock.Advance(TimeSpan.FromSeconds(1));
            _commitCancellation.ThrowIfCancellationRequested();
        };
    }

    Task Because() => ExecuteCatching();

    [Fact] void should_preserve_the_unknown_commit() => _failure.ShouldBeOfExactType<NamespaceCommitOutcomeUnknown>();
    [Fact] void should_not_retry() => _attempts.ShouldEqual(1);
    [Fact] void should_not_abort_a_possibly_committed_transaction() => _aborts.ShouldEqual(0);
    [Fact] void should_not_back_off() => _delays.ShouldBeEmpty();
}

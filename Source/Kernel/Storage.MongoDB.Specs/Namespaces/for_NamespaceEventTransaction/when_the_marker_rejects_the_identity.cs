// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_marker_rejects_the_identity : given.a_namespace_transaction
{
    void Establish()
    {
        _matches = 0;
        _context = _context with { ActivationIncarnation = null };
    }
    Task Because() => ExecuteCatching();

    [Fact] void should_report_a_stale_or_closed_incarnation() => _failure.ShouldBeOfExactType<NamespaceCommitRejected>();
    [Fact] void should_never_write_events_or_receipts() => _writeSessions.ShouldBeEmpty();
    [Fact] void should_abort_without_attempting_commit() => _steps.SequenceEqual(["fence", "abort"]).ShouldBeTrue();
    [Fact] void should_not_retry() => _attempts.ShouldEqual(1);
}

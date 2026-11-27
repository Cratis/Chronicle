// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_caller_deadline_expires_before_commit : given.a_namespace_transaction
{
    void Establish()
    {
        _context = _context with { Deadline = _clock.GetUtcNow() + TimeSpan.FromSeconds(1) };
        _duringWrite = () => _clock.Advance(TimeSpan.FromSeconds(1));
    }

    Task Because() => ExecuteCatching();

    [Fact] void should_report_definite_pre_commit_cancellation() => _failure.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_abort_the_staged_work() => _aborts.ShouldEqual(1);
    [Fact] void should_not_submit_commit() => _commits.ShouldEqual(0);
    [Fact] void should_not_retry() => _attempts.ShouldEqual(1);
}

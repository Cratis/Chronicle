// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_caller_deadline_expires_during_backoff : given.a_namespace_transaction
{
    void Establish()
    {
        _context = _context with { Deadline = _clock.GetUtcNow() + TimeSpan.FromSeconds(1) };
        _fenceFailure = CommandFailure(112);
        _fenceFailures = 1;
        _duringDelay = () => _clock.Advance(TimeSpan.FromSeconds(1));
    }

    Task Because() => ExecuteCatching();

    [Fact] void should_honor_the_original_deadline() => _failure.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_start_a_second_attempt() => _attempts.ShouldEqual(1);
    [Fact] void should_not_commit() => _commits.ShouldEqual(0);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_a_commit_is_acknowledged_after_the_deadline : given.a_namespace_transaction
{
    void Establish()
    {
        _context = _context with { Deadline = _clock.GetUtcNow() + TimeSpan.FromSeconds(1) };
        _duringCommit = () => _clock.Advance(TimeSpan.FromSeconds(1));
    }

    Task Because() => Execute();

    [Fact] void should_return_the_definite_acknowledgement() => _result.ShouldEqual(42);
    [Fact] void should_not_relabel_a_known_commit_as_aborted() => _commits.ShouldEqual(1);
    [Fact] void should_not_abort_or_retry() => _aborts.ShouldEqual(0);
    [Fact] void should_not_back_off() => _delays.ShouldBeEmpty();
}

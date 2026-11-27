// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_write_conflicts_exhaust_the_budget : given.a_namespace_transaction
{
    void Establish()
    {
        _fenceFailure = CommandFailure(112);
        _fenceFailures = 4;
    }

    Task Because() => ExecuteCatching();

    [Fact] void should_stop_after_three_attempts() => _attempts.ShouldEqual(3);
    [Fact] void should_preserve_the_definite_abort_failure() => _failure.ShouldEqual(_fenceFailure);
    [Fact] void should_abort_every_attempt() => _aborts.ShouldEqual(3);
    [Fact] void should_not_commit_any_events() => _commits.ShouldEqual(0);
}

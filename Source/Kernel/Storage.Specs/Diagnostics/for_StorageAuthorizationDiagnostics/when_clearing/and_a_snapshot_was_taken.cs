// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Diagnostics.for_StorageAuthorizationDiagnostics.when_clearing;

public class and_a_snapshot_was_taken : given.a_registry
{
    IReadOnlyCollection<StorageAuthorizationFailure> _snapshot;

    void Establish()
    {
        _registry.Record(_failure);
        _snapshot = _registry.Failures;
    }

    void Because() => _registry.Clear("System", "EventTypeStatistics");

    [Fact] void should_preserve_the_snapshot() => _snapshot.ShouldContainOnly(_failure);
    [Fact] void should_remove_the_current_failure() => _registry.Failures.ShouldBeEmpty();
}

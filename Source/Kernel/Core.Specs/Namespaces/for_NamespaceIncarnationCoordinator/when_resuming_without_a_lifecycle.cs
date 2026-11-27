// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_resuming_without_a_lifecycle : given.a_recovery_operation
{
    Exception _error;

    void Establish() => _state = null!;

    async Task Because() => _error = await Catch.Exception(() => _coordinator.Resume(_original.OperationId, _original.Revision));

    [Fact] void should_refuse_the_missing_operation() => _error.ShouldBeOfExactType<NamespaceLifecycleChanged>();
    [Fact] void should_not_write() => _writes.ShouldBeEmpty();
    [Fact] void should_not_issue_an_effect() => _order.ShouldBeEmpty();
}

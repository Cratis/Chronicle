// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_resuming_with_a_stale_revision : given.a_recovery_operation
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _coordinator.Resume(_original.OperationId, _original.Revision - 1));

    [Fact] void should_refuse_the_stale_request() => _error.ShouldBeOfExactType<NamespaceLifecycleChanged>();
    [Fact] void should_not_write() => _writes.ShouldBeEmpty();
    [Fact] void should_not_issue_any_effect() => _order.ShouldBeEmpty();
}

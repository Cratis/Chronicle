// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_aborting_after_a_destructive_action : given.a_recovery_operation
{
    Exception _error;

    void Establish() => _state = _state with { DestructiveActionIssued = true };

    async Task Because() => _error = await Catch.Exception(() => _coordinator.Abort(_state.OperationId, _state.Revision));

    [Fact] void should_refuse_the_abort() => _error.ShouldBeOfExactType<NamespaceRemovalCannotBeAborted>();
    [Fact] void should_not_write() => _writes.ShouldBeEmpty();
    [Fact] void should_not_open_any_marker() => _order.ShouldBeEmpty();
}

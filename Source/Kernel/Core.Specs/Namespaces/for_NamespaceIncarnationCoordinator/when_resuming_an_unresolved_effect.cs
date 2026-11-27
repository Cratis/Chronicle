// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_resuming_an_unresolved_effect : given.a_recovery_operation
{
    Exception _error;

    void Establish() => _state = _state with { AttemptInProgress = true, DestructiveAttemptOutstanding = true };

    async Task Because() => _error = await Catch.Exception(() => _coordinator.Resume(_state.OperationId, _state.Revision));

    [Fact] void should_require_terminal_reconciliation() => _error.ShouldBeOfExactType<NamespaceRecoveryRequired>();
    [Fact] void should_not_write() => _writes.ShouldBeEmpty();
    [Fact] void should_not_start_a_competing_effect() => _order.ShouldBeEmpty();
}

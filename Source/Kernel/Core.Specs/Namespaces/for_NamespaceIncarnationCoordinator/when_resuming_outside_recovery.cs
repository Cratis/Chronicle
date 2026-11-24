// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_resuming_outside_recovery : given.a_recovery_operation
{
    Exception _error;

    void Establish() => _state = _state with { Status = NamespaceLifecycleStatus.Closing };

    async Task Because() => _error = await Catch.Exception(() => _coordinator.Resume(_state.OperationId, _state.Revision));

    [Fact] void should_refuse_the_wrong_status() => _error.ShouldBeOfExactType<NamespaceLifecycleChanged>();
    [Fact] void should_not_write() => _writes.ShouldBeEmpty();
    [Fact] void should_not_issue_an_effect() => _order.ShouldBeEmpty();
}

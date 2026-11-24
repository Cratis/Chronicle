// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_resuming_with_another_operation : given.a_recovery_operation
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _coordinator.Resume(Guid.NewGuid(), _original.Revision));

    [Fact] void should_refuse_the_other_operation() => _error.ShouldBeOfExactType<NamespaceLifecycleChanged>();
    [Fact] void should_not_write() => _writes.ShouldBeEmpty();
}

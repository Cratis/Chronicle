// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_aborting_pre_destructive_recovery : given.a_recovery_operation
{
    async Task Because()
    {
        await _coordinator.Abort(_original.OperationId, _original.Revision);
        _result = await _coordinator.Ensure(true);
    }

    [Fact] void should_open_a_new_lifetime() => _result.Status.ShouldEqual(NamespaceLifecycleStatus.Open);
    [Fact] void should_never_reopen_the_old_identity() => _result.Incarnation.ShouldNotEqual(_original.Incarnation);
    [Fact] void should_retain_the_retired_identity() => _result.PreviousIncarnation.ShouldEqual(_original.Incarnation);
    [Fact] void should_not_emit_a_second_creation_notification() => _order.ShouldNotContain("Create");
    [Fact] void should_close_before_opening_the_replacement() => _order.IndexOf("CloseMarker").ShouldBeLessThan(_order.IndexOf("OpenMarker"));
}

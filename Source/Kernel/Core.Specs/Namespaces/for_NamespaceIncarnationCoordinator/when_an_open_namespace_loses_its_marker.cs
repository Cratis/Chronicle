// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_an_open_namespace_loses_its_marker : given.a_coordinator
{
    Exception _error;

    void Establish() => _state = NamespaceLifecycle.Open(_name, NamespaceIncarnationId.New(), _clock.GetUtcNow());

    async Task Because() => _error = await Catch.Exception(() => _coordinator.Ensure(true));

    [Fact] void should_fail_closed_instead_of_recreating_a_marker() => _error.ShouldBeOfExactType<NamespaceRecoveryRequired>();
    [Fact] void should_require_explicit_reconciliation() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.RecoveryRequired);
    [Fact] void should_record_the_diagnostic() => _state.LastFailure.ShouldEqual("NamespaceOpenMarkerMismatch");
    [Fact] void should_not_upsert_a_marker() => _order.Contains("OpenMarker").ShouldBeFalse();
}

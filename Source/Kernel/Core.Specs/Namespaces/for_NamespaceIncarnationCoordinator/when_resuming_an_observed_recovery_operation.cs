// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_resuming_an_observed_recovery_operation : given.a_recovery_operation
{
    NamespaceLifecycle _attempt;

    async Task Because()
    {
        _result = await _coordinator.Resume(_original.OperationId, _original.Revision);
        _attempt = _result.BeginAttempt(_clock.GetUtcNow());
    }

    [Fact] void should_retain_the_operation() => _result.OperationId.ShouldEqual(_original.OperationId);
    [Fact] void should_retain_the_incarnation() => _result.Incarnation.ShouldEqual(_original.Incarnation);
    [Fact] void should_retain_the_historical_attempts() => _result.TotalAttempts.ShouldEqual(5);
    [Fact] void should_authorize_a_new_bounded_budget() => _attempt.TotalAttempts.ShouldEqual(6);
    [Fact] void should_allow_the_new_attempt() => _attempt.Status.ShouldEqual(NamespaceLifecycleStatus.Closing);
    [Fact] void should_increment_the_revision() => _result.Revision.ShouldEqual(_original.Revision + 1);
}

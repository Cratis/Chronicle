// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceLifecycle;

public class when_restarting_after_an_issued_drop : Specification
{
    NamespaceLifecycle _before;
    NamespaceLifecycle _after;

    void Establish()
    {
        var closing = NamespaceLifecycle.Open("orders", NamespaceIncarnationId.New(), DateTimeOffset.UnixEpoch)
            .BeginRemoval(Guid.NewGuid(), DateTimeOffset.UnixEpoch) with
        { Stage = NamespaceRemovalStage.Drop };
        _before = closing.BeginAttempt(DateTimeOffset.UnixEpoch);
    }

    void Because() => _after = _before.RecoverOutstandingAttempt(DateTimeOffset.UnixEpoch.AddSeconds(1));

    [Fact] void should_require_operator_recovery() => _after.Status.ShouldEqual(NamespaceLifecycleStatus.RecoveryRequired);
    [Fact] void should_preserve_the_attempt_count() => _after.StageAttempts.ShouldEqual(_before.StageAttempts);
    [Fact] void should_preserve_the_operation() => _after.OperationId.ShouldEqual(_before.OperationId);
    [Fact] void should_preserve_the_incarnation() => _after.Incarnation.ShouldEqual(_before.Incarnation);
    [Fact] void should_keep_the_drop_outstanding() => _after.DestructiveAttemptOutstanding.ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceLifecycle;

public class when_recording_a_drop_attempt : Specification
{
    NamespaceLifecycle _before;
    NamespaceLifecycle _after;

    void Establish() => _before = NamespaceLifecycle.Open("orders", NamespaceIncarnationId.New(), DateTimeOffset.UnixEpoch)
        .BeginRemoval(Guid.NewGuid(), DateTimeOffset.UnixEpoch) with
    { Stage = NamespaceRemovalStage.Drop };

    void Because() => _after = _before.BeginAttempt(DateTimeOffset.UnixEpoch.AddSeconds(1));

    [Fact] void should_account_for_the_attempt_before_issuing_it() => _after.StageAttempts.ShouldEqual(1);
    [Fact] void should_mark_the_destructive_attempt_outstanding() => _after.DestructiveAttemptOutstanding.ShouldBeTrue();
    [Fact] void should_advance_the_revision_for_compare_and_swap() => _after.Revision.ShouldEqual(_before.Revision + 1);
    [Fact] void should_keep_the_removal_identity() => _after.OperationId.ShouldEqual(_before.OperationId);
}

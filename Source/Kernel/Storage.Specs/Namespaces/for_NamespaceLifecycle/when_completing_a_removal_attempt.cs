// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceLifecycle;

public class when_completing_a_removal_attempt : Specification
{
    NamespaceLifecycle _state;

    void Establish() => _state = NamespaceLifecycle.Open("orders", NamespaceIncarnationId.New(), DateTimeOffset.UnixEpoch)
        .BeginRemoval(Guid.NewGuid(), DateTimeOffset.UnixEpoch)
        .BeginAttempt(DateTimeOffset.UnixEpoch);

    void Because()
    {
        for (var stage = NamespaceRemovalStage.CloseMarker; stage < NamespaceRemovalStage.Complete; stage++)
        {
            _state = _state.CompleteStage(DateTimeOffset.UnixEpoch);
            if (_state.Status == NamespaceLifecycleStatus.Closing)
            {
                _state = _state.IssueStage();
            }
        }
    }

    [Fact] void should_acknowledge_all_stages_before_removing() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.Removed);
    [Fact] void should_consume_one_attempt_for_the_operation_not_one_per_stage() => _state.TotalAttempts.ShouldEqual(1);
    [Fact] void should_retain_that_destructive_actions_were_issued() => _state.DestructiveActionIssued.ShouldBeTrue();
    [Fact] void should_have_no_unacknowledged_destructive_effect() => _state.DestructiveAttemptOutstanding.ShouldBeFalse();
    [Fact] void should_finish_the_attempt() => _state.AttemptInProgress.ShouldBeFalse();
}

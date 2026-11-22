// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalRecovery;

public class when_all_stages_are_acknowledged : given.a_removal
{
    Task Because() => _recovery.Tick(_name);

    [Fact] void should_execute_in_checkpoint_order() => _effects.ShouldEqual([NamespaceRemovalStage.CloseMarker, NamespaceRemovalStage.Forget, NamespaceRemovalStage.Cleanup, NamespaceRemovalStage.Drop, NamespaceRemovalStage.DeleteListing]);
    [Fact] void should_persist_each_effect_before_issuing_it() => _issued.TrueForAll(_ => _.AttemptInProgress).ShouldBeTrue();
    [Fact] void should_persist_destructive_intent_before_forget_and_later_stages() => _issued.Skip(1).All(_ => _.DestructiveAttemptOutstanding).ShouldBeTrue();
    [Fact] void should_finish_removed() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.Removed);
    [Fact] void should_count_one_attempt_for_the_entire_removal() => _state.TotalAttempts.ShouldEqual(1);
}

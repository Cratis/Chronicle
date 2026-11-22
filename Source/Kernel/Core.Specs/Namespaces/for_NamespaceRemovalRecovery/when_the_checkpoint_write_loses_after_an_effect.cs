// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalRecovery;

public class when_the_checkpoint_write_loses_after_an_effect : given.a_removal
{
    void Establish() => _execute = _ =>
    {
        _state = _state.RequireRecovery("ChangedByAnotherOwner");
        return Task.CompletedTask;
    };

    Task Because() => _recovery.Tick(_name);

    [Fact] void should_not_issue_a_later_stage() => _effects.ShouldEqual([NamespaceRemovalStage.CloseMarker]);
    [Fact] void should_not_overwrite_the_newer_owner_state() => _state.LastFailure.ShouldEqual("ChangedByAnotherOwner");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalRecovery;

public class when_the_control_write_loses : given.a_removal
{
    void Establish() => _storage.TryWrite(_name, Arg.Any<long?>(), Arg.Any<NamespaceLifecycle>()).Returns(Task.FromResult(false));

    Task Because() => _recovery.Tick(_name);

    [Fact] void should_not_issue_an_unrecorded_attempt() => _effects.Count.ShouldEqual(0);
    [Fact] void should_leave_the_control_state_alone() => _state.TotalAttempts.ShouldEqual(0);
}

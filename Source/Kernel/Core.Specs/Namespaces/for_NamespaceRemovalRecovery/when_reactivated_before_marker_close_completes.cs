// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalRecovery;

public class when_reactivated_before_marker_close_completes : given.a_removal
{
    void Establish() => _state = _state.BeginAttempt(_clock.GetUtcNow());

    Task Because() => CreateRecovery().Tick(_name);

    [Fact] void should_consume_another_attempt_before_reissuing_the_idempotent_close() => _issued[0].TotalAttempts.ShouldEqual(2);
    [Fact] void should_complete_the_removal() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.Removed);
}

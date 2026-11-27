// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalRecovery;

public class when_retry_is_not_due : given.a_removal
{
    void Establish() => _state = _state.BeginAttempt(_clock.GetUtcNow()).FailAttempt("MarkerUnavailable", _clock.GetUtcNow());

    Task Because() => CreateRecovery().Tick(_name);

    [Fact] void should_not_issue_an_early_retry() => _effects.Count.ShouldEqual(0);
    [Fact] void should_not_consume_the_next_attempt() => _state.TotalAttempts.ShouldEqual(1);
}

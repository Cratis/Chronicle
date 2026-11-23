// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep;

/// <summary>
/// The operator command job concludes only once the replay job it started is no longer outstanding.
/// </summary>
public class when_waiting_for_a_replay_to_finish : Specification
{
    int _checks;

    async Task Because() => await ObserverOperatorCommandStep.WaitForReplay(() => Task.FromResult(++_checks < 3), TimeSpan.Zero, CancellationToken.None);

    [Fact] void should_keep_checking_until_the_replay_is_no_longer_outstanding() => _checks.ShouldEqual(3);
}

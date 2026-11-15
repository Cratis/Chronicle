// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_Occurrences.when_registering_again_after_reconnect;

public class and_it_runs_twice : given.a_registered_occurrence_client
{
    async Task Because()
    {
        await _lifecycle.Disconnected();
        await Task.WhenAll(_lifecycle.Connected(), _occurrences.RegisterAgainAfterReconnect(), _occurrences.RegisterAgainAfterReconnect());
        await _observedAgain.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
    }

    [Fact] void should_start_only_one_new_worker() => _observations.ShouldEqual(2);
}

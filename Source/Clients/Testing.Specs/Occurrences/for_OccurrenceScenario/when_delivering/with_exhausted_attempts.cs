// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceScenario.when_delivering;

public class with_exhausted_attempts : given.a_scenario
{
    async Task Because()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            await _scenario.DeliverNextFailing(new ScenarioFailure());
            var snapshot = (await _scenario.StatusOf(_recorded.Id))!;
            if (attempt < 9) _scenario.Time.AdvanceTo(snapshot.NextAttempt);
        }
    }

    [Fact] void should_collect_quarantine() => _scenario.ShouldBeQuarantined(_recorded.Id);

    sealed class ScenarioFailure : Exception;
}

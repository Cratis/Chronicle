// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Configuration.for_ShutdownStageTimings;

public class when_the_budget_expires_while_a_stage_is_stopping : given.a_stage_timings_logger
{
    async Task Because()
    {
        var highest = Enumerable.Range(0, _stages.Count).OrderByDescending(_ => _stages[_]).First();
        await _observers[highest].OnStop(CancellationToken.None);
        _time.Advance(TimeSpan.FromSeconds(8));
        await _deadline.CancelAsync();
    }

    [Fact] void should_warn_naming_the_stage_still_stopping() => _logged.ShouldContain(_ => _.Level == LogLevel.Warning && _.Message.Contains("still stopping", StringComparison.Ordinal));
}

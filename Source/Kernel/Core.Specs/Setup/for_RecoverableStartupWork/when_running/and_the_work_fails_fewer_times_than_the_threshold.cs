// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_RecoverableStartupWork.when_running;

public class and_the_work_fails_fewer_times_than_the_threshold : given.a_recoverable_startup_work
{
    int _attempts;

    async Task Because() => await _work.Run(
        _item,
        _ => ++_attempts < RecoverableStartupWork.EscalateAfterFailures ? Task.FromException(new TimeoutException()) : Task.CompletedTask,
        CancellationToken.None);

    [Fact] void should_not_escalate() => _escalation.Escalations.ShouldBeEmpty();
    [Fact] void should_not_report_a_recovery() => _escalation.Recoveries.ShouldBeEmpty();
}

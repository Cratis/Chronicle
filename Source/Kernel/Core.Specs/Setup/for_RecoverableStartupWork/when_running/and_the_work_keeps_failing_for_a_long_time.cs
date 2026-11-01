// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_RecoverableStartupWork.when_running;

public class and_the_work_keeps_failing_for_a_long_time : given.a_recoverable_startup_work
{
    const int Failures = 60;

    int _attempts;

    void Establish() => _jitter = 0;

    async Task Because() => await _work.Run(
        _item,
        _ => ++_attempts <= Failures ? Task.FromException(new TimeoutException()) : Task.CompletedTask,
        CancellationToken.None);

    [Fact] void should_escalate_again() => _escalation.Escalations.Count.ShouldBeGreaterThan(1);
    [Fact] void should_not_escalate_on_every_failure() => _escalation.Escalations.Count.ShouldBeLessThan(Failures - RecoverableStartupWork.EscalateAfterFailures + 1);
    [Fact] void should_start_escalating_at_the_threshold() => _escalation.Escalations[0].Attempts.ShouldEqual(RecoverableStartupWork.EscalateAfterFailures);
    [Fact] void should_leave_at_least_the_backoff_cap_between_repeats() => _escalation.Escalations.Zip(_escalation.Escalations.Skip(1), (previous, next) => next.Attempts - previous.Attempts).All(_ => _ >= 2).ShouldBeTrue();
    [Fact] void should_report_the_recovery() => _escalation.Recoveries.ShouldContainOnly([(_item, Failures + 1)]);
}

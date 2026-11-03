// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_RecoverableStartupWork.when_running;

public class and_the_work_fails_then_succeeds : given.a_recoverable_startup_work
{
    const int Failures = 8;

    int _attempts;

    async Task Because() => await _work.Run(
        _item,
        _ => ++_attempts <= Failures ? Task.FromException(new TimeoutException()) : Task.CompletedTask,
        CancellationToken.None);

    [Fact] void should_have_tried_until_it_succeeded() => _attempts.ShouldEqual(Failures + 1);
    [Fact] void should_escalate_once_at_the_threshold() => _escalation.Escalations.Select(_ => (_.Item, _.Attempts)).ShouldContainOnly([(_item, RecoverableStartupWork.EscalateAfterFailures)]);
    [Fact] void should_not_repeat_the_escalation_within_the_cap_interval() => _escalation.Escalations.Count.ShouldEqual(1);
    [Fact] void should_report_the_recovery() => _escalation.Recoveries.ShouldContainOnly([(_item, Failures + 1)]);
}

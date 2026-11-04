// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameAudit;

public class when_a_read_succeeds_after_the_time_budget : given.an_audit
{
    Exception _result;

    async Task Because()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<IEnumerable<ResourceUse>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _inventory.Read(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            started.TrySetResult();
            return finish.Task;
        });

        var run = _audit.RunAsync(NameCollisionHandling.Quarantine, _budget, _ => false, CancellationToken.None);

        // The read is under way, so the deadline passes while it is still running, and only then does it succeed.
        await started.Task;
        _time.Advance(_budget + TimeSpan.FromSeconds(1));
        finish.SetResult([Use("orders", "db-1")]);

        _result = await Catch.Exception(() => run);
    }

    [Fact] void should_throw_that_the_audit_failed() => _result.ShouldBeOfExactType<ResourceNameAuditFailed>();
    [Fact] void should_not_be_ready() => _snapshot.Check("orders").ShouldEqual(ResourceNameStatus.NotReady);
}

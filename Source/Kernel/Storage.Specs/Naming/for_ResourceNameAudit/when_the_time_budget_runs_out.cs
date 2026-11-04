// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameAudit;

public class when_the_time_budget_runs_out : given.an_audit
{
    Exception _result;

    void Establish() =>
        _inventory.Read(Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<IEnumerable<ResourceUse>>(new TimeoutException()));

    async Task Because() => _result = await Catch.Exception(() => Run(timeout: TimeSpan.FromMinutes(1), isTransient: _ => true));

    [Fact] void should_throw_that_the_audit_failed() => _result.ShouldBeOfExactType<ResourceNameAuditFailed>();
    [Fact] void should_keep_the_last_failure() => _result.InnerException.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_not_be_ready() => _snapshot.Check("orders").ShouldEqual(ResourceNameStatus.NotReady);
}

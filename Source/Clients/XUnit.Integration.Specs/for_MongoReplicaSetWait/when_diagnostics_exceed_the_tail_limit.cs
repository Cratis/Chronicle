// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoReplicaSetWait;

public class when_diagnostics_exceed_the_tail_limit : given.a_readiness_wait
{
    Exception _failure;

    void Establish()
    {
        var output = "old output\n" + string.Join('\n', Enumerable.Repeat(new string('x', 500), 80)) + "\nlatest output";
        _container.ExecAsync(Arg.Any<IList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ExecResult(output, string.Empty, 1));
        _container.GetLogsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((output, string.Empty));
    }

    async Task Because()
    {
        await _wait.UntilAsync(_container);
        _clock.Advance(MongoReplicaSetWait.ReadinessTimeout);
        _failure = await Catch.Exception(() => _wait.UntilAsync(_container));
    }

    [Fact] void should_discard_old_output() => _failure.Message.ShouldNotContain("old output");
    [Fact] void should_keep_latest_output() => _failure.Message.ShouldContain("latest output");
    [Fact] void should_bound_the_total_diagnostic_size() => (_failure.Message.Length < 32500).ShouldBeTrue();
    [Fact] void should_bound_the_number_of_lines() => (_failure.Message.Split('\n').Length < 130).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoReplicaSetWait;

public class when_readiness_transport_and_logs_fail : given.a_readiness_wait
{
    Exception _failure;

    async Task Because()
    {
        await _wait.UntilAsync(_container);
        _container.ExecAsync(Arg.Any<IList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ExecResult>(new IOException("Docker exec unavailable")));
        _container.GetLogsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<(string, string)>(new IOException("Docker logs unavailable")));
        await _wait.UntilAsync(_container);
        _clock.Advance(MongoReplicaSetWait.ReadinessTimeout);
        _failure = await Catch.Exception(() => _wait.UntilAsync(_container));
    }

    [Fact] void should_keep_the_readiness_failure_as_primary() => _failure.ShouldBeOfExactType<MongoReplicaSetReadinessFailed>();
    [Fact] void should_keep_the_last_completed_status() => _failure.Message.ShouldContain("{ ok: 1, myState: 2 }");
    [Fact] void should_report_the_failed_probe() => _failure.Message.ShouldContain("Readiness probe failed: IOException");
    [Fact] void should_report_unavailable_logs() => _failure.Message.ShouldContain("[container logs unavailable: IOException]");
    [Fact] void should_still_include_the_container_identity() => _failure.Message.ShouldContain("Container=mongo-123");
}

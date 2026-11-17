// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoReplicaSetWait;

public class when_replica_set_initialization_fails : given.a_readiness_wait
{
    Exception _failure;

    void Establish() => _container.ExecAsync(Arg.Any<IList<string>>(), Arg.Any<CancellationToken>())
        .Returns(new ExecResult("MongoServerError: InvalidReplicaSetConfig\nMongoDB rs.initiate exit code: 1\n{ ok: 0 }", string.Empty, 1));

    async Task Because() => _failure = await Catch.Exception(() => _wait.UntilAsync(_container));

    [Fact] void should_fail_on_the_first_probe_without_advancing_the_clock() => _failure.ShouldBeOfExactType<MongoReplicaSetReadinessFailed>();
    [Fact] void should_identify_initialization_as_the_failure() => _failure.Message.ShouldContain("MongoDB rs.initiate failed.");
    [Fact] void should_include_the_container_identity() => _failure.Message.ShouldContain("Container=mongo-123");
    [Fact] void should_include_the_image() => _failure.Message.ShouldContain("mongo:8.2");
    [Fact] void should_include_initialization_output() => _failure.Message.ShouldContain("InvalidReplicaSetConfig");
    [Fact] void should_include_the_exit_code() => _failure.Message.ShouldContain("MongoDB rs.initiate exit code: 1");
    [Fact] void should_include_the_log_tail() => _failure.Message.ShouldContain("replica-set election stalled");
    [Fact] void should_probe_only_once() => _container.Received(1).ExecAsync(Arg.Any<IList<string>>(), Arg.Any<CancellationToken>());
}

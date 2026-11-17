// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_MongoReplicaSetWait;

public class when_the_readiness_deadline_expires : given.a_readiness_wait
{
    bool _ready;
    Exception _failure;

    async Task Because()
    {
        _ready = await _wait.UntilAsync(_container);
        _clock.Advance(MongoReplicaSetWait.ReadinessTimeout);
        _failure = await Catch.Exception(() => _wait.UntilAsync(_container));
    }

    [Fact] void should_keep_waiting_before_the_deadline() => _ready.ShouldBeFalse();
    [Fact] void should_fail_without_triggering_generic_timeout_retries() => _failure.ShouldBeOfExactType<MongoReplicaSetReadinessFailed>();
    [Fact] void should_include_the_container_identity() => _failure.Message.ShouldContain("Container=mongo-123");
    [Fact] void should_include_the_image() => _failure.Message.ShouldContain("mongo:8.2");
    [Fact] void should_include_the_deadline() => _failure.Message.ShouldContain("150 seconds");
    [Fact] void should_include_initialization_output() => _failure.Message.ShouldContain("rs.initiate exit code: 1");
    [Fact] void should_include_the_last_status() => _failure.Message.ShouldContain("{ ok: 1, myState: 2 }");
    [Fact] void should_include_readiness_standard_error() => _failure.Message.ShouldContain("not primary yet");
    [Fact] void should_include_the_log_tail() => _failure.Message.ShouldContain("replica-set election stalled");
    [Fact] void should_include_log_standard_error() => _failure.Message.ShouldContain("init failed");
    [Fact] void should_not_probe_after_the_deadline() => _container.Received(1).ExecAsync(Arg.Any<IList<string>>(), Arg.Any<CancellationToken>());
}

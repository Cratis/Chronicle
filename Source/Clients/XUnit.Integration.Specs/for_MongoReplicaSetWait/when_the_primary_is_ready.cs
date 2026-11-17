// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoReplicaSetWait;

public class when_the_primary_is_ready : given.a_readiness_wait
{
    bool _ready;

    void Establish() => _container.ExecAsync(Arg.Any<IList<string>>(), Arg.Any<CancellationToken>())
        .Returns(new ExecResult("{ ok: 1, myState: 1 }", string.Empty, 0));

    async Task Because() => _ready = await _wait.UntilAsync(_container);

    [Fact] void should_accept_the_primary() => _ready.ShouldBeTrue();
    [Fact] void should_not_collect_failure_logs() => _container.DidNotReceive().GetLogsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
}

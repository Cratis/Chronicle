// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog;

public class when_starting_twice : given.a_connection_watchdog
{
    async Task Because()
    {
        _watchDog.Start();
        _watchDog.Start();
        await Task.Delay(50);
        await _cancellationTokenSource.CancelAsync();
    }

    [Fact] void should_only_run_one_monitor() => _tasks.Received(1).Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
}

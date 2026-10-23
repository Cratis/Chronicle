// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelHost;

public class when_shutdown_is_requested_at_the_active_stage : given.a_held_silo
{
    Exception? _error;

    async Task Because()
    {
        await StartHeld();
        await _startup.CancelAsync();
        _error = await Catch.Exception(() => _start.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Fact] void should_cancel_startup_without_an_unhandled_failure() => (_error is OperationCanceledException).ShouldBeTrue();
}

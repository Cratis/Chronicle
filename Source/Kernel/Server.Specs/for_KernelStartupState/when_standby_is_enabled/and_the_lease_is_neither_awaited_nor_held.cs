// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupState.when_standby_is_enabled;

/// <summary>
/// Covers a lost lease and an acquisition that gave up: neither may leave the kernel looking ready.
/// </summary>
public class and_the_lease_is_neither_awaited_nor_held : given.a_kernel_startup_state
{
    bool _standby;

    void Because() => _standby = _state.IsStandby;

    [Fact] void should_not_be_in_standby() => _standby.ShouldBeFalse();
}

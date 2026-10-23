// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupGate;

public class when_ready : given.a_starting_kernel
{
    void Establish()
    {
        _started.Cancel();
    }

    Task Because() => _gate.InvokeAsync(_context);

    [Fact] void should_pass_through() => _passedThrough.ShouldBeTrue();
}

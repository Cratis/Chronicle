// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupGate.when_starting;

public class and_ready_is_requested : given.a_starting_kernel
{
    void Establish()
    {
        _context.Request.Path = "/health/ready";
    }

    Task Because() => _gate.InvokeAsync(_context);

    [Fact] void should_pass_through_to_the_readiness_check() => _passedThrough.ShouldBeTrue();
}

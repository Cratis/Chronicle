// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_KernelStartupGate.when_starting;

public class and_the_health_port_is_exclusive : given.a_starting_kernel
{
    void Establish()
    {
        _options = new ChronicleOptions { Health = new Health { Port = 8080, Exclusive = true } };
        _context.Connection.LocalPort = 8080;
        _context.Request.Path = "/health";
    }

    Task Because() => _gate.InvokeAsync(_context);

    [Fact] void should_pass_through() => _passedThrough.ShouldBeTrue();
}

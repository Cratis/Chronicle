// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_KernelStartupGate.when_starting;

public class and_a_custom_health_path_is_requested : given.a_starting_kernel
{
    void Establish()
    {
        _options = new ChronicleOptions { HealthCheckEndpoint = "/probe" };
        _context.Request.Path = "/probe/live";
    }

    Task Because() => _gate.InvokeAsync(_context);

    [Fact] void should_pass_through() => _passedThrough.ShouldBeTrue();
}

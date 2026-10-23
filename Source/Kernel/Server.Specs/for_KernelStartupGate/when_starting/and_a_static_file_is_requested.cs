// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupGate.when_starting;

public class and_a_static_file_is_requested : given.a_starting_kernel
{
    void Establish()
    {
        _context.Request.Path = "/index.html";
    }

    Task Because() => _gate.InvokeAsync(_context);

    [Fact] void should_refuse_the_request() => _context.Response.StatusCode.ShouldEqual(503);
}

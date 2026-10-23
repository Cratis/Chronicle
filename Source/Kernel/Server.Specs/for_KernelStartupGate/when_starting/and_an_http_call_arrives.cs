// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupGate.when_starting;

public class and_an_http_call_arrives : given.a_starting_kernel
{
    Task Because() => _gate.InvokeAsync(_context);

    [Fact] void should_refuse_the_request() => _context.Response.StatusCode.ShouldEqual(503);
    [Fact] void should_offer_a_retry() => _context.Response.Headers.RetryAfter.ToString().ShouldEqual("1");
    [Fact] void should_not_enter_the_pipeline() => _passedThrough.ShouldBeFalse();
}

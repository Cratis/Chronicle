// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupGate.when_starting;

public class and_a_grpc_call_arrives : given.a_starting_kernel
{
    void Establish()
    {
        _context.Request.ContentType = "application/grpc+proto";
    }

    Task Because() => _gate.InvokeAsync(_context);

    [Fact] void should_use_http_success() => _context.Response.StatusCode.ShouldEqual(200);
    [Fact] void should_report_unavailable() => _context.Response.Headers.GrpcStatus.ToString().ShouldEqual("14");
    [Fact] void should_not_enter_the_pipeline() => _passedThrough.ShouldBeFalse();
}

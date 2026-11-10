// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SingleWriter;

namespace Cratis.Chronicle.Server.for_KernelStartupGate.when_in_standby;

public class and_a_grpc_call_arrives : given.a_kernel_in_standby
{
    void Establish() => _context.Request.ContentType = "application/grpc";

    Task Because() => _gate.InvokeAsync(_context);

    [Fact] void should_answer_at_once_with_a_trailers_only_response() => _context.Response.StatusCode.ShouldEqual(200);
    [Fact] void should_report_unavailable_so_clients_retry() => _context.Response.Headers.GrpcStatus.ToString().ShouldEqual("14");
    [Fact] void should_explain_the_standby() => _context.Response.Headers.GrpcMessage.ToString().ShouldEqual(SingleWriterLeaseStandby.RefusalMessage);
    [Fact] void should_not_enter_the_pipeline() => _passedThrough.ShouldBeFalse();
}

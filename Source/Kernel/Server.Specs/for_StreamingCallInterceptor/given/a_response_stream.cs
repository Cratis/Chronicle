// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services;
using Grpc.Core;

namespace Cratis.Chronicle.Server.for_StreamingCallInterceptor.given;

public class a_response_stream : Specification
{
    internal StreamingCallInterceptor _interceptor;
    protected ServerCallContext _context;
    protected RecordingResponseStreamWriter _writer;
    protected RpcException _failure;
    protected Exception _error;

    void Establish()
    {
        _interceptor = new();
        _context = Substitute.ForPartsOf<ServerCallContext>();
        _writer = new();
        _failure = new(new Status(StatusCode.PermissionDenied, "Refused"));
    }

    protected void RecordFailure() => _context.UserState[StreamingCallFailures.Key] = _failure;
}

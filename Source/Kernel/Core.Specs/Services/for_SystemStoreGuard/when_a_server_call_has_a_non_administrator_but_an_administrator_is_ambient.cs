// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services.for_QueryExecutor;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_a_server_call_has_a_non_administrator_but_an_administrator_is_ambient : given.an_in_process_request
{
    CallContext _context;
    HttpContext _serverRequest;

    void Establish()
    {
        _serverRequest = new DefaultHttpContext { User = CallerNamed(OrdinaryUser), RequestServices = _httpContext.RequestServices };
        _context = new(new object(), new FakeServerCallContext(_serverRequest));
    }

    async Task Because() => await Evaluate(_context);

    [Fact] void should_use_the_server_request() => SystemStoreGuard.HttpContextFor(_context).ShouldEqual(_serverRequest);
    [Fact] void should_not_use_the_ambient_administrator() => _allowed.ShouldBeFalse();
    [Fact] void should_refuse_the_system_store() => Refusal.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}

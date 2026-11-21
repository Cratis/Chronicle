// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services.for_QueryExecutor;
using Microsoft.AspNetCore.Http;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_a_server_administrator_has_an_ordinary_ambient_user : given.an_in_process_request
{
    CallContext _context;

    void Establish()
    {
        var serverRequest = new DefaultHttpContext { User = CallerNamed(Administrator), RequestServices = _httpContext.RequestServices };
        _context = new(new object(), new FakeServerCallContext(serverRequest));
        _httpContext.User = CallerNamed(OrdinaryUser);
    }

    async Task Because() => await Evaluate(_context);

    [Fact] void should_allow_the_server_administrator() => _allowed.ShouldBeTrue();
    [Fact] void should_not_refuse_the_system_store() => _refusal.ShouldBeNull();
}

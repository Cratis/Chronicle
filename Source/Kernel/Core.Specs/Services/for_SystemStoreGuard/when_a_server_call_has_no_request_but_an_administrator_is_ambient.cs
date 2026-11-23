// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services.for_QueryExecutor;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_a_server_call_has_no_request_but_an_administrator_is_ambient : given.an_in_process_request
{
    CallContext _context;

    void Establish() => _context = new(new object(), new FakeServerCallContext(null));

    async Task Because() => await Evaluate(_context);

    [Fact] void should_not_substitute_the_ambient_administrator() => _allowed.ShouldBeFalse();
    [Fact] void should_refuse_the_system_store() => Refusal.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services.for_QueryExecutor;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

[Collection("SystemStoreGuard")]
public class when_a_server_call_has_no_http_context : for_SystemStoreGuard.given.an_in_process_request
{
    IAuthorizationDecisions _decisions;
    bool _permitted;

    void Establish() => _decisions = _httpContext.RequestServices.GetRequiredService<IAuthorizationDecisions>();

    async Task Because()
    {
        BeginRequest();
        _permitted = await StoreCapabilityGuard.Permits(
            new CallContext(new object(), new FakeServerCallContext(null)), new QueryRouting("shop", "tenant"));
    }

    [Fact] void should_refuse_without_using_the_ambient_administrator() => _permitted.ShouldBeFalse();
    [Fact] void should_not_use_the_ambient_request_services() => _decisions.ReceivedCalls().ShouldBeEmpty();
}

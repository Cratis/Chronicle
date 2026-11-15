// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services.for_QueryExecutor;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

public class when_a_server_call_has_no_http_context
{
    [Fact]
    public async Task should_refuse_without_using_in_process_services()
    {
        var previous = SystemStoreGuard.InProcessServices;
        var decisions = Substitute.For<IAuthorizationDecisions>();
        await using var services = new ServiceCollection().AddSingleton(decisions).BuildServiceProvider();
        try
        {
            SystemStoreGuard.InProcessServices = services;
            var context = new CallContext(new object(), new FakeServerCallContext(null));
            (await StoreCapabilityGuard.Permits(context, new QueryRouting("store", "ns"))).ShouldBeFalse();
            decisions.ReceivedCalls().ShouldBeEmpty();
        }
        finally
        {
            SystemStoreGuard.InProcessServices = previous;
        }
    }
}

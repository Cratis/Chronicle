// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services.for_QueryExecutor;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard.given;

public class a_request_with_forged_credentials : Cratis.Chronicle.Security.given.a_system_store_caller
{
    protected bool _allowed;
    protected RpcException? _refusal;

    protected CallContext ContextWithForgedHeaders()
    {
        var http = CreateServices().GetRequiredService<IHttpContextAccessor>().HttpContext!;
        http.Request.Headers["X-Admin"] = "true";
        http.Request.Headers["X-Roles"] = "administrator,chronicle-admin";
        http.Request.Headers["X-Claim-Roles"] = "chronicle-admin";
        http.Request.Headers["X-Forwarded-User"] = _administratorId.ToString();
        http.Request.Headers.Authorization = "Bearer not.a.validated-token";
        return new(new object(), new FakeServerCallContext(http));
    }

    protected async Task Evaluate(CallContext context)
    {
        _allowed = await SystemStoreGuard.IsCallerAllowed(context);
        _refusal = await Catch.Exception(() => SystemStoreGuard.Ensure(context, new QueryRouting("System", null))) as RpcException;
    }
}

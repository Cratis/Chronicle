// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard.given;

[Collection("SystemStoreGuard")]
public class an_in_process_request : Chronicle.Security.given.a_system_store_caller
{
    protected HttpContext _httpContext;
    protected IHttpContextAccessor _accessor;
    protected bool _allowed;
    protected HttpContext? _resolvedRequest;
    protected Exception? _refusal;
    IServiceProvider? _previousServices;
    HttpContext? _previousHttpContext;
    IServiceProvider _services;

    protected virtual Authentication AuthenticationOptions => new();

    protected virtual bool HasAmbientRequest => true;

    protected RpcException Refusal => (RpcException)_refusal!;

    protected void BeginRequest() => _accessor.HttpContext = HasAmbientRequest ? _httpContext : null;

    protected async Task Evaluate(CallContext context = default)
    {
        BeginRequest();
        _resolvedRequest = SystemStoreGuard.HttpContextFor(context);
        _allowed = await SystemStoreGuard.IsCallerAllowed(context);
        _refusal = await Catch.Exception(() => SystemStoreGuard.Ensure(context, new QueryRouting("System", null)));
    }

    void Establish()
    {
        _previousServices = SystemStoreGuard.InProcessServices;
        _accessor = new HttpContextAccessor();
        _previousHttpContext = _accessor.HttpContext;
        _authentication = AuthenticationOptions;
        _services = CreateServices();
        _httpContext = _accessor.HttpContext!;
        SystemStoreGuard.InProcessServices = _services;
    }

    void Destroy()
    {
        _accessor.HttpContext = _previousHttpContext;
        SystemStoreGuard.InProcessServices = _previousServices;
        (_services as IDisposable)?.Dispose();
    }
}

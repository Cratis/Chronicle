// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_concurrently.given;

public class a_delegate_with_an_http_request : a_delegate_with_gated_batches
{
    protected readonly HttpContextAccessor _accessor = new();
    protected readonly CancellationTokenSource _requestAborted = new();
    protected readonly TaskCompletionSource<Seen> _seenByHandler = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected ServiceProvider _requestServices;
    CancellationToken _handlerToken;

    protected record Seen(bool HasHttpContext, bool HasRequestMarker, bool HasUser, bool HasTenantHeader, bool HandlerCancelled);

    protected class RequestMarker;

    void Establish()
    {
        _requestServices = new ServiceCollection().AddScoped<RequestMarker>().BuildServiceProvider();
    }

    void Destroy()
    {
        _requestAborted.Dispose();
        _requestServices.Dispose();
    }

    protected void BeginRequest()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = _requestServices.CreateScope().ServiceProvider,
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice")], "test")),
            RequestAborted = _requestAborted.Token
        };
        context.Request.Headers["x-tenant"] = "tenant-1";
        _accessor.HttpContext = context;
    }

    protected void EndRequest() => _accessor.HttpContext = null;

    static bool HasMarker(HttpContext? context)
    {
        try
        {
            return context?.RequestServices.GetService<RequestMarker>() is not null;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    protected override void OnHandling(string name, CancellationToken cancellationToken) => _handlerToken = cancellationToken;

    protected override void OnHandled(string name)
    {
        var context = _accessor.HttpContext;
        _seenByHandler.TrySetResult(new Seen(
            context is not null,
            HasMarker(context),
            context?.User.Identity?.IsAuthenticated == true,
            context?.Request.Headers.ContainsKey("x-tenant") == true,
            _handlerToken.IsCancellationRequested));
    }
}

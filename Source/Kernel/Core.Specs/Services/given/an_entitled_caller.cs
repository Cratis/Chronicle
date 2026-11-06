// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services.for_QueryExecutor;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.given;

public sealed class an_entitled_caller : IDisposable
{
    readonly ServiceProvider _services;
    readonly StoreEntitlementsCache _cache;

    public an_entitled_caller(bool enforce = true, string? ns = null, CancellationToken cancellationToken = default)
    {
        Clock = new EntitlementClock();
        var grain = Substitute.For<IStoreEntitlements>();
        grain.GetVersion().Returns(1L);
        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(grain);
        _cache = new(grains, NullLogger<StoreEntitlementsCache>.Instance, Clock);
        _cache.Publish(new(1, [new(new(PrincipalKey.LocalIssuer, "app"), "store", ns is null ? null : new Concepts.EventStoreNamespaceName(ns), StoreAccess.Read | StoreAccess.Append | StoreAccess.Manage, EventStoreAccessGrantSource.Command)]));
        var options = Options.Create(new ChronicleOptions { Authentication = new Authentication { Enabled = true, EnforceStoreEntitlements = enforce } });
        var accessor = new HttpContextAccessor();
        _services = new ServiceCollection()
            .AddSingleton<IHttpContextAccessor>(accessor)
            .AddSingleton<IStoreEntitlementsCache>(_cache)
            .AddSingleton<TimeProvider>(Clock)
            .AddSingleton<IAuthorizationDecisions>(new AuthorizationDecisions(Substitute.For<IAdministratorAuthorization>(), options, NullLogger<AuthorizationDecisions>.Instance, _cache))
            .AddSingleton(options)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .BuildServiceProvider();
        Http = new DefaultHttpContext { RequestServices = _services, User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "app")], "Bearer")) };
        accessor.HttpContext = Http;
        Context = new(new object(), new FakeServerCallContext(Http, cancellationToken));
    }

    public CallContext Context { get; }
    public HttpContext Http { get; }
    public EntitlementClock Clock { get; }
    public void Revoke() => _cache.Publish(new(2, []));
    public Task ConfirmLease() => _cache.Poll();

    public void Dispose()
    {
        _services.Dispose();
        _cache.Dispose();
    }
}

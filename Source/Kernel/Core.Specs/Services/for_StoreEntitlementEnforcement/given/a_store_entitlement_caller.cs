// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Subjects;
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

namespace Cratis.Chronicle.Services.for_StoreEntitlementEnforcement.given;

public class a_store_entitlement_caller : Specification
{
    protected IAuthorizationDecisions _decisions;
    protected IAdministratorPrivileges _privileges;
    protected IGrainFactory _grains;
    protected IStoreEntitlementsCache _cache;
    protected Subject<Unit> _changes;
    protected List<(Capability Capability, AuthorizationScope Scope)> _asked;
    protected bool _granted;

    protected CallContext ContextFor(bool enforce)
    {
        var services = new ServiceCollection()
            .AddSingleton(_decisions)
            .AddSingleton(_privileges)
            .AddSingleton(_grains)
            .AddSingleton(_cache)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(Options.Create(new ChronicleOptions { Authentication = new Authentication { EnforceStoreEntitlements = enforce } }))
            .BuildServiceProvider();
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "someone")], "spec")),
            RequestServices = services
        };
        return new(new object(), new FakeServerCallContext(http, default));
    }

    void Establish()
    {
        _asked = [];
        _granted = false;
        _changes = new();
        _privileges = Substitute.For<IAdministratorPrivileges>();
        _grains = Substitute.For<IGrainFactory>();
        _cache = Substitute.For<IStoreEntitlementsCache>();
        _cache.IsFresh.Returns(true);
        _cache.Changes.Returns(_changes);
        _decisions = Substitute.For<IAuthorizationDecisions>();
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>()).Returns(call =>
        {
            _asked.Add((call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
            return _granted
                ? AuthorizationDecision.Allow(AuthorizationReason.Granted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2))
                : AuthorizationDecision.Deny(AuthorizationReason.NotGranted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2));
        });
    }
}

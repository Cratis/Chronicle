// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.given;

public class a_system_store_caller : Specification
{
    public const string Administrator = "administrator";
    public const string RevokedAdministrator = "revoked administrator";
    public const string OrdinaryUser = "ordinary user";
    public const string Application = "application";
    public const string Anonymous = "anonymous";

    protected UserId _administratorId;
    protected UserId _ordinaryUserId;
    protected UserId _revokedAdministratorId;
    protected IAdministratorPrivileges _privileges;
    protected Authentication _authentication;
    protected ClaimsPrincipal? _caller;

    public static TheoryData<string> NonAdministrators() => new() { RevokedAdministrator, OrdinaryUser, Application, Anonymous };

    protected IServiceProvider CreateServices()
    {
        var authorization = new AdministratorAuthorization(
            Options.Create(new ChronicleOptions { Authentication = _authentication }), _privileges, NullLogger<AdministratorAuthorization>.Instance);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = _caller } };
        var services = new ServiceCollection()
            .AddSingleton<IHttpContextAccessor>(accessor)
            .AddSingleton<IAdministratorAuthorization>(authorization)
            .AddSingleton<ISystemStoreAccess>(new SystemStoreAccessVerdict(new AuthorizationDecisions(Options.Create(new ChronicleOptions { Authentication = _authentication }), NullLogger<AuthorizationDecisions>.Instance, httpContextAccessor: accessor)))
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(StoreDecisions())
            .BuildServiceProvider();
        accessor.HttpContext.RequestServices = services;
        return services;
    }

    /// <summary>Entitles every caller to the stores they address, as a store-scoped decision is not what these specs are about.</summary>
    /// <returns>The decisions.</returns>
    static IAuthorizationDecisions StoreDecisions()
    {
        var decisions = Substitute.For<IAuthorizationDecisions>();
        decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>()).Returns(call =>
            AuthorizationDecision.Allow(AuthorizationReason.Granted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
        return decisions;
    }

    protected ClaimsPrincipal CallerNamed(string name) => name switch
    {
        Administrator => User(_administratorId),
        RevokedAdministrator => User(_revokedAdministratorId),
        OrdinaryUser => User(_ordinaryUserId),
        Application => new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "spec")),
        _ => new ClaimsPrincipal(new ClaimsIdentity())
    };

    static ClaimsPrincipal User(Guid id) => new(new ClaimsIdentity(
        [new Claim("sub", id.ToString()), new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)], "spec"));

    void Establish()
    {
        _administratorId = Guid.NewGuid();
        _ordinaryUserId = Guid.NewGuid();
        _revokedAdministratorId = Guid.NewGuid();
        _privileges = Substitute.For<IAdministratorPrivileges>();
        _privileges.IsActiveAdministrator(_administratorId).Returns(true);
        _privileges.IsActiveAdministrator(_ordinaryUserId).Returns(false);
        _privileges.IsActiveAdministrator(_revokedAdministratorId).Returns(false);
        _authentication = new Authentication();
        _caller = CallerNamed(Administrator);
    }
}

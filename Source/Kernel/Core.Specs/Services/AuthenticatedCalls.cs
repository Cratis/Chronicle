// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services.for_QueryExecutor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Creates the <see cref="CallContext"/> of calls made by callers with a given identity.
/// </summary>
public static class AuthenticatedCalls
{
    /// <summary>
    /// Gets the owner a call made by the given subject has.
    /// </summary>
    /// <param name="subject">The subject of the caller.</param>
    /// <returns>The <see cref="ConnectionOwner"/>.</returns>
    public static ConnectionOwner OwnerOf(string subject) => new("issuer", subject);

    /// <summary>
    /// Creates the context of a call made by an authenticated caller.
    /// </summary>
    /// <param name="subject">The subject of the caller.</param>
    /// <param name="administrator">Whether the caller is an administrator.</param>
    /// <param name="cancellationToken">The optional token that ends the call.</param>
    /// <returns>The <see cref="CallContext"/>.</returns>
    public static CallContext As(string subject, bool administrator = false, CancellationToken cancellationToken = default)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject, ClaimValueTypes.String, "issuer")], "test"));
        return Served(principal, administrator, cancellationToken);
    }

    /// <summary>
    /// Gets the owner a call made by the given client has.
    /// </summary>
    /// <param name="client">The client identifier.</param>
    /// <returns>The <see cref="ConnectionOwner"/>.</returns>
    public static ConnectionOwner OwnerOfClient(string client) => new("issuer", client, true);

    /// <summary>
    /// Creates the context of a call made by an authenticated caller that has a client identifier but no subject.
    /// </summary>
    /// <param name="client">The client identifier.</param>
    /// <returns>The <see cref="CallContext"/>.</returns>
    public static CallContext AsClient(string client)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("client_id", client, ClaimValueTypes.String, "issuer")], "test"));
        return Served(principal, false, default);
    }

    /// <summary>
    /// Creates the context of a call whose request carries no authenticated principal.
    /// </summary>
    /// <param name="cancellationToken">The optional token that ends the call.</param>
    /// <returns>The <see cref="CallContext"/>.</returns>
    public static CallContext Unauthenticated(CancellationToken cancellationToken = default) =>
        Served(new ClaimsPrincipal(new ClaimsIdentity()), false, cancellationToken);

    /// <summary>
    /// Creates the context of a server call that carries no request.
    /// </summary>
    /// <param name="cancellationToken">The optional token that ends the call.</param>
    /// <returns>The <see cref="CallContext"/>.</returns>
    public static CallContext WithoutRequest(CancellationToken cancellationToken = default) =>
        new(new object(), new FakeServerCallContext(null, cancellationToken));

    static CallContext Served(ClaimsPrincipal principal, bool administrator, CancellationToken cancellationToken)
    {
        var authorization = Substitute.For<IAuthorizationService>();
        authorization
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(administrator ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        var decisions = Substitute.For<IAuthorizationDecisions>();
        decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>()).Returns(call =>
            AuthorizationDecision.Allow(AuthorizationReason.Granted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
        var services = new ServiceCollection()
            .AddSingleton(authorization)
            .AddSingleton(decisions)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .BuildServiceProvider();
        var http = new DefaultHttpContext { User = principal, RequestServices = services };
        return new(new object(), new FakeServerCallContext(http, cancellationToken));
    }
}

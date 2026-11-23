// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_a_non_administrator_reads_system_statistics : given.an_http_statistics_reader
{
    protected override Authentication AuthenticationOptions => new()
    {
        Http = new() { Mode = HttpAuthenticationMode.TrustedProxy, TrustedProxy = new() { AdministratorGroup = "administrators" } }
    };

    public static TheoryData<string, bool> Callers() => new()
    {
        { "ordinary", false }, { "ordinary", true },
        { "revoked", false }, { "revoked", true },
        { "application", false }, { "application", true },
        { "anonymous", false }, { "anonymous", true },
        { "proxy-user", false }, { "proxy-user", true },
        { "forged-headers", false }, { "forged-headers", true }
    };

    [Theory]
    [MemberData(nameof(Callers))]
    public async Task should_refuse_before_reading_statistics(string caller, bool storeWide)
    {
        BeginRequest();
        SetCaller(caller);
        var exception = await Catch.Exception(async () =>
        {
            if (storeWide) await EventStatistics.StatisticsForEventStore(_reader, "System");
            else await EventStatistics.StatisticsForNamespace(_reader, "System", "first");
        });

        ((RpcException)exception!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        _sink.ReceivedCalls().ShouldBeEmpty();
        _secondSink.ReceivedCalls().ShouldBeEmpty();
    }

    void SetCaller(string caller)
    {
        _httpContext.User = caller switch
        {
            "ordinary" => new(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)], "spec")),
            "application" => new(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "spec")),
            "proxy-user" => new(new ClaimsIdentity([new Claim(TrustedProxyUsers.GroupClaimType, "readers")], TrustedProxyUsers.AuthenticationType)),
            "revoked" => _httpContext.User,
            _ => new(new ClaimsIdentity())
        };
        if (caller == "revoked") _privileges.IsActiveAdministrator(_administratorId).Returns(false);
        if (caller != "forged-headers") return;
        _httpContext.Request.Headers["x-ms-client-principal-name"] = "admin";
        _httpContext.Request.Headers["x-ms-client-principal-groups"] = "administrators";
        _httpContext.Request.Headers["x-cratis-roles"] = "admin";
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions.when_deciding_store_access;

[PerFactSpecificationLifecycle]
public class with_a_trusted_proxy_user : Specification
{
    IAdministratorPrivileges _privileges;
    IStoreEntitlementsCache _cache;

    void Establish()
    {
        _privileges = Substitute.For<IAdministratorPrivileges>();
        _cache = Substitute.For<IStoreEntitlementsCache>();
        _cache.Snapshot.Returns(StoreEntitlementsSnapshot.Empty);
        _cache.IsFresh.Returns(true);
    }

    [Theory]
    [InlineData("chronicle.event-store.read")]
    [InlineData("chronicle.event-store.append")]
    [InlineData("chronicle.event-store.manage")]
    public async Task should_leave_them_unenrolled_like_a_workbench_user(string capability) =>
        (await Decisions(Proxied()).Decide(ProxyUser(), new(capability), AuthorizationScope.ForEventStore("shop"))).Reason.ShouldEqual(AuthorizationReason.NotEnrolled);

    [Fact]
    public async Task should_not_trust_a_proxy_identity_when_the_mode_is_off() =>
        (await Decisions(new Authentication { Enabled = true, Authority = "https://idp.example" }).Decide(ProxyUser(), WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop"))).Reason.ShouldEqual(AuthorizationReason.NotGranted);

    [Theory]
    [InlineData("chronicle-admins", true)]
    [InlineData("operators", false)]
    public async Task should_make_members_of_the_administrator_group_administrators(string group, bool expected) =>
        (await Decisions(Proxied("chronicle-admins")).Decide(ProxyUser(group), WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel)).IsAllowed.ShouldEqual(expected);

    [Fact]
    public async Task should_make_every_proxy_user_an_administrator_without_an_administrator_group() =>
        (await Decisions(Proxied()).Decide(ProxyUser(), WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel)).IsAllowed.ShouldBeTrue();

    [Fact]
    public async Task should_never_consult_the_roster()
    {
        await Decisions(Proxied()).Decide(ProxyUser(), WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel);
        _privileges.ReceivedCalls().ShouldBeEmpty();
    }

    static Authentication Proxied(string? administratorGroup = null) => new()
    {
        Enabled = true,
        Http = new HttpAuthentication
        {
            Mode = HttpAuthenticationMode.TrustedProxy,
            TrustedProxy = new TrustedProxyAuthentication { AdministratorGroup = administratorGroup }
        }
    };

    static ClaimsPrincipal ProxyUser(params string[] groups) => new(new ClaimsIdentity(
        [
            new Claim("sub", "alice", ClaimValueTypes.String, TrustedProxyAuthentication.DefaultIssuer),
            .. groups.Select(_ => new Claim(TrustedProxyUsers.GroupClaimType, _, ClaimValueTypes.String, TrustedProxyAuthentication.DefaultIssuer))
        ],
        TrustedProxyUsers.AuthenticationType));

    AuthorizationDecisions Decisions(Authentication authentication)
    {
        var options = Options.Create(new ChronicleOptions { Authentication = authentication });
        return new(options, NullLogger<AuthorizationDecisions>.Instance, _cache, Security.given.caller_services.For(new AdministratorAuthorization(options, _privileges, NullLogger<AdministratorAuthorization>.Instance)));
    }
}

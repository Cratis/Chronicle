// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions.when_deciding_store_access;

public class with_an_ordinary_store : Specification
{
    IAdministratorPrivileges _privileges;
    IGrainFactory _grains;
    IStoreEntitlementsCache _cache;
    Authentication _authentication;
    ClaimsPrincipal _app;

    void Establish()
    {
        _privileges = Substitute.For<IAdministratorPrivileges>();
        _grains = Substitute.For<IGrainFactory>();
        _cache = Substitute.For<IStoreEntitlementsCache>();
        _authentication = new Authentication { Enabled = true };
        _app = Principal("app", false);
        _cache.Snapshot.Returns(new StoreEntitlementsSnapshot(new(1, [new(PrincipalKey.From(_app, _authentication), "shop", null, StoreAccess.Read, EventStoreAccessGrantSource.Command)])));
        _cache.IsFresh.Returns(true);
    }

    [Theory]
    [InlineData("chronicle.event-store.read", AuthorizationReason.Granted)]
    [InlineData("chronicle.event-store.append", AuthorizationReason.NotGranted)]
    [InlineData("chronicle.event-store.manage", AuthorizationReason.NotGranted)]
    public async Task should_decide_apps_without_authoritative_calls(string capability, AuthorizationReason expected)
    {
        var task = Decisions().Decide(_app, new(capability), AuthorizationScope.ForEventStore("shop"));
        task.IsCompletedSuccessfully.ShouldBeTrue();
        (await task).Reason.ShouldEqual(expected);
        _privileges.ReceivedCalls().ShouldBeEmpty();
        _grains.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("chronicle.event-store.read")]
    [InlineData("chronicle.event-store.append")]
    [InlineData("chronicle.event-store.manage")]
    public async Task should_leave_local_users_unenrolled(string capability) =>
        (await Decisions().Decide(Principal("user", true), new(capability), AuthorizationScope.ForEventStore("shop"))).Reason.ShouldEqual(AuthorizationReason.NotEnrolled);

    [Theory]
    [InlineData(false, AuthorizationReason.NotGranted)]
    [InlineData(true, AuthorizationReason.Failed)]
    public async Task should_never_honor_an_expired_snapshot(bool enforce, AuthorizationReason expected)
    {
        var authentication = new Authentication { Enabled = true, EnforceStoreEntitlements = enforce };
        _cache.IsFresh.Returns(false);
        (await Decisions(authentication).Decide(_app, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop"))).Reason.ShouldEqual(expected);
    }

    [Theory]
    [InlineData(false, AuthorizationReason.NotGranted)]
    [InlineData(true, AuthorizationReason.Failed)]
    public async Task should_handle_an_unloaded_snapshot(bool enforce, AuthorizationReason expected)
    {
        var authentication = new Authentication { Enabled = true, EnforceStoreEntitlements = enforce };
        _cache.Snapshot.Returns(StoreEntitlementsSnapshot.Empty);
        _cache.IsFresh.Returns(false);
        (await Decisions(authentication).Decide(_app, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop"))).Reason.ShouldEqual(expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_require_authoritative_administrator_authority_for_stale_cross_store_key_grants(bool administrator)
    {
        var id = Guid.NewGuid();
        var principal = Principal(id.ToString(), true);
        _cache.Snapshot.Returns(new StoreEntitlementsSnapshot(new(1, [new(PrincipalKey.From(principal, _authentication), "other", null, StoreAccess.Manage, EventStoreAccessGrantSource.Command)])));
        _cache.IsFresh.Returns(false);
        _privileges.IsActiveAdministrator(id).Returns(administrator);
        var result = await Decisions().Decide(principal, WellKnownCapabilities.ManageComplianceKeys, AuthorizationScope.ForEventStore("other"));
        result.IsAllowed.ShouldEqual(administrator);
        await _privileges.Received(1).IsActiveAdministrator(id);
    }

    [Fact] async Task should_reject_an_unidentified_caller() => (await Decisions().Decide(new(new ClaimsIdentity([], "Bearer")), WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop"))).Reason.ShouldEqual(AuthorizationReason.NoPrincipal);
    [Fact] async Task should_reject_an_anonymous_caller() => (await Decisions().Decide(null, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop"))).Reason.ShouldEqual(AuthorizationReason.NoPrincipal);

    [Fact]
    async Task should_allow_with_authentication_disabled()
    {
        var authentication = new Authentication { Enabled = false };
        (await Decisions(authentication).Decide(null, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop"))).Reason.ShouldEqual(AuthorizationReason.AuthenticationDisabled);
    }

    [Theory]
    [InlineData("chronicle.compliance.release")]
    [InlineData("chronicle.compliance.key-lifecycle")]
    [InlineData("chronicle.alerts.read")]
    public async Task should_keep_the_administrator_check_for_local_users(string capability)
    {
        var id = Guid.NewGuid();
        _privileges.IsActiveAdministrator(id).Returns(true);
        (await Decisions().Decide(Principal(id.ToString(), true), new(capability), AuthorizationScope.ForEventStore("shop"))).Reason.ShouldEqual(AuthorizationReason.Granted);
        await _privileges.Received(1).IsActiveAdministrator(id);
    }

    [Fact]
    async Task should_keep_the_system_administrator_branch() =>
        (await Decisions().Decide(_app, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("System"))).Reason.ShouldEqual(AuthorizationReason.NotGranted);

    [Fact]
    async Task should_not_match_a_local_grant_under_an_external_authority()
    {
        var authentication = new Authentication { Enabled = true, Authority = "https://other" };
        (await Decisions(authentication).Decide(Principal("app", false, "https://other"), WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop"))).Reason.ShouldEqual(AuthorizationReason.NotGranted);
    }

    [Fact]
    async Task should_allow_an_explicit_external_administrator_claim()
    {
        var authentication = new Authentication { Enabled = true, Authority = "https://other", ExternalAdministratorClaim = new() { Type = "role", Value = "operator" } };
        var principal = Principal("external", false, "https://other");
        ((ClaimsIdentity)principal.Identity).AddClaim(new("role", "operator", ClaimValueTypes.String, "https://other"));
        (await Decisions(authentication).Decide(principal, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop"))).Reason.ShouldEqual(AuthorizationReason.Granted);
    }

    [Fact]
    public async Task should_recheck_authority_even_when_reusing_a_completed_result()
    {
        var decisions = Decisions();
        var scope = decisions.Inputs.Scope("shop", null);
        var first = decisions.Decide(_app, WellKnownCapabilities.ReadEventStore, scope);
        var second = decisions.Decide(Principal("app", false), WellKnownCapabilities.ReadEventStore, scope);
        ReferenceEquals(first, second).ShouldBeTrue();
        (await decisions.Decide(Principal("other", false), WellKnownCapabilities.ReadEventStore, scope)).IsAllowed.ShouldBeFalse();
        _cache.IsFresh.Returns(false);
        (await decisions.Decide(_app, WellKnownCapabilities.ReadEventStore, scope)).IsAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task should_refuse_if_the_lease_expires_while_evaluating_a_grant()
    {
        _cache.IsFresh.Returns(true, false);
        (await Decisions().Decide(_app, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop"))).IsAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task should_refuse_if_the_snapshot_changes_while_evaluating_a_grant()
    {
        var previous = _cache.Snapshot;
        _cache.Snapshot.Returns(previous, new StoreEntitlementsSnapshot(new(2, [])));
        (await Decisions().Decide(_app, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop"))).IsAllowed.ShouldBeFalse();
    }

    AuthorizationDecisions Decisions(Authentication? authentication = null)
    {
        var options = Options.Create(new ChronicleOptions { Authentication = authentication ?? _authentication });
        return new(new AdministratorAuthorization(options, _privileges, NullLogger<AdministratorAuthorization>.Instance), options, NullLogger<AuthorizationDecisions>.Instance, _cache);
    }

    static ClaimsPrincipal Principal(string subject, bool user, string issuer = ClaimsIdentity.DefaultIssuer) => new(new ClaimsIdentity(
        [new Claim("sub", subject, ClaimValueTypes.String, issuer), new Claim(ChronicleClaimTypes.Principal, user ? ChronicleClaimTypes.User : "application", ClaimValueTypes.String, issuer)], issuer == ClaimsIdentity.DefaultIssuer ? "Bearer" : ExternalAuthority.AuthenticationType));
}

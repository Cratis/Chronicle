// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService;

public class when_deciding_key_lifecycle_targets : given.a_compliance_boundary
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_mutate_only_allowed_stores_and_report_denied_and_failed_coverage(bool allowNew)
    {
        Deny(OtherEventStore);
        Deny(ThirdEventStore, throws: true);
        var notCovered = await Operate(allowNew);
        notCovered.ShouldEqual([OtherEventStore.Value, ThirdEventStore.Value]);
        ShouldNotHaveTouched(OtherEventStore);
        ShouldNotHaveTouched(ThirdEventStore);
        ShouldNotHaveTouched(EventStoreName.System);
        if (allowNew)
        {
            await _keyStore.Received(1).AllowNewKeyFor(EventStore, EventStoreNamespace, Identifier);
        }
        else
        {
            await _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
            _keys.ShouldContain(OtherEventStore);
            _keys.ShouldContain(ThirdEventStore);
            _keys.ShouldNotContain(EventStore);
            _allDecidedBeforeMutation.ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_without_a_store_list_and_change_no_store_when_the_requested_store_is_denied(bool allowNew)
    {
        Deny(EventStore);
        var exception = await Catch.Exception(() => Operate(allowNew));
        exception.ShouldBeOfExactType<RpcException>();
        ((RpcException)exception).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        ShouldNotHaveTouched(EventStore);
        ShouldNotHaveTouched(OtherEventStore);
        ShouldNotHaveTouched(ThirdEventStore);
        _audit.Entries.ShouldEqual(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_eligible_fan_out_in_audit_mode_but_never_include_system(bool allowNew)
    {
        Configure(enforce: false);
        Deny(OtherEventStore);
        Deny(ThirdEventStore, throws: true);
        (await Operate(allowNew)).ShouldBeEmpty();
        foreach (var store in new[] { EventStore, OtherEventStore, ThirdEventStore })
        {
            if (allowNew) await _keyStore.Received(1).AllowNewKeyFor(store, EventStoreNamespace, Identifier);
            else await _keyStore.Received(1).DeleteFor(store, EventStoreNamespace, Identifier);
        }
        ShouldNotHaveTouched(EventStoreName.System);
        ShouldNotHaveTouched("system");
        _audit.Entries.ShouldEqual(1);
        string.Join(' ', _audit.Values).ShouldContain("audit");
    }

    [Fact]
    public async Task should_decide_each_distinct_non_system_target_in_the_requested_namespace_using_the_captured_principal()
    {
        await Operate(allowNew: false);
        _decisions.ReceivedCalls().Count().ShouldEqual(3);
        foreach (var store in new[] { EventStore, OtherEventStore, ThirdEventStore })
        {
            await _decisions.Received(1).Decide(
                Arg.Is<ClaimsPrincipal>(principal => principal != _principal && principal.FindFirst("sub").Value == "compliance-caller"),
                WellKnownCapabilities.ManageComplianceKeys,
                AuthorizationScope.ForNamespace(store, EventStoreNamespace));
        }
        _allDecidedBeforeMutation.ShouldBeTrue();
    }

    [Fact]
    public async Task should_log_one_sanitized_summary_including_failed_decisions()
    {
        Deny(ThirdEventStore, throws: true);
        await Operate(allowNew: false);
        _audit.Entries.ShouldEqual(1);
        var values = string.Join(' ', _audit.Values);
        values.ShouldContain("delete-key");
        values.ShouldContain("enforce");
        values.ShouldContain("test-issuer");
        values.ShouldContain(ThirdEventStore.Value);
        values.ShouldContain("Failed");
        values.ShouldNotContain(Identifier.Value);
        values.ShouldNotContain(Key.Fingerprint);
        values.ShouldNotContain(Convert.ToBase64String(Key.Public));
        values.ShouldNotContain(Convert.ToBase64String(Key.Private));
        values.ShouldNotContain("sensitive lookup detail");
        foreach (var exception in _audit.Exceptions) exception.ShouldBeNull();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService;

public class when_the_principal_changes_while_listing_targets : given.a_compliance_boundary
{
    void Establish() => _storage.GetEventStores().Returns(_ =>
    {
        var identity = (ClaimsIdentity)_principal.Identity!;
        identity.RemoveClaim(identity.FindFirst("sub"));
        identity.AddClaim(new Claim("sub", "replacement-caller"));
        return Task.FromResult<IEnumerable<EventStoreName>>([EventStore]);
    });

    Task Because() => Operate(allowNew: false);

    [Fact]
    void should_decide_for_the_detached_principal_captured_before_the_first_await() =>
        _decisions.Received(1).Decide(
            Arg.Is<ClaimsPrincipal>(principal => principal.FindFirst("sub")!.Value == "compliance-caller"),
            WellKnownCapabilities.ManageComplianceKeys,
            AuthorizationScope.ForNamespace(EventStore, EventStoreNamespace));

    [Fact] void should_have_actually_changed_the_request_principal() => _principal.FindFirst("sub")!.Value.ShouldEqual("replacement-caller");
}

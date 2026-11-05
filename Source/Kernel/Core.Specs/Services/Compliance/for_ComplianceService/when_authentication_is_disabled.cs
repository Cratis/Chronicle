// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService;

public class when_authentication_is_disabled : given.a_compliance_boundary
{
    string[] _notCovered;

    void Establish()
    {
        Configure(enforce: true, authenticationEnabled: false);
        _decisions = new AuthorizationDecisions(
            Substitute.For<IAdministratorAuthorization>(),
            Options.Create(new ChronicleOptions { Authentication = new() { Enabled = false } }),
            NullLogger<AuthorizationDecisions>.Instance);
        UseCaller(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    async Task Because()
    {
        _notCovered = await Operate(allowNew: false);
        await _service.Release(ReleaseRequest(strict: false), _context);
    }

    [Fact] void should_cover_all_eligible_stores() => _notCovered.ShouldBeEmpty();
    [Fact] void should_erase_the_requested_store() => _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_erase_the_other_store() => _keyStore.Received(1).DeleteFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_log_the_authentication_disabled_decision() => string.Join(' ', _audit.Values).ShouldContain("AuthenticationDisabled");
}

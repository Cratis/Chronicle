// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;
using StoredAdministrator = Cratis.Chronicle.Storage.Security.Administrator;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_a_grant_and_revocation_are_replayed_after_recovery : given.an_administrators_reactor
{
    EventContext _grantContext;
    EventContext _revocationContext;
    UserRevokedAdministrator _revoked;
    UserGrantedAdministrator _recovered;
    IEnumerable<StoredAdministrator> _privileges;
    IEnumerable<StoredAdministrator> _afterRevocation;

    async Task Establish()
    {
        _grantContext = SeedGrant();
        await _reactor.Granted(_grant, _grantContext);
        _revoked = new UserRevokedAdministrator(_userId, "operator revocation");
        _revocationContext = Seed(_grantContext.EventSourceId, _revoked);
        await _reactor.Revoked(_revoked, _revocationContext);
        _afterRevocation = await _storage.System.Administrators.GetAll();
        _recovered = new UserGrantedAdministrator(_userId, GrantReason.OperatorRecovery, "new recovery nonce");
        await _reactor.Granted(_recovered, Seed(_grantContext.EventSourceId, _recovered));
    }

    async Task Because()
    {
        await _reactor.Granted(_grant, _grantContext);
        await _reactor.Revoked(_revoked, _revocationContext);
        _privileges = await _storage.System.Administrators.GetAll();
    }

    [Fact] void should_remove_an_ordered_revocation() => _afterRevocation.ShouldBeEmpty();
    [Fact] void should_preserve_the_latest_grant() => _privileges.Single().ShouldEqual(new StoredAdministrator(_userId, _recovered.Reason, _recovered.Evidence));
}

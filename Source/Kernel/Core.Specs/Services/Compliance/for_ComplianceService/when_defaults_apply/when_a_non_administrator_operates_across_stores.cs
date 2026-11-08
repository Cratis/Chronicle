// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Compliance;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService.when_defaults_apply;

public class when_a_non_administrator_operates_across_stores : given.a_compliance_boundary
{
    void Establish()
    {
        Configure(enforce: false, useDefaults: true);
        HoldIdentifierOnlyIn(EventStore, OtherEventStore);
        Deny(OtherEventStore);
        Deny(ThirdEventStore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_audit_denials_and_cover_every_store(bool allowNew)
    {
        (await Operate(allowNew)).ShouldBeEmpty();
        foreach (var store in new[] { EventStore, OtherEventStore, ThirdEventStore })
        {
            if (allowNew) await _keyStore.Received(1).AuthorizeConfirmedSuccessorFor(store, EventStoreNamespace, Identifier);
            else await _keyStore.Received(1).DeleteThroughFor(store, EventStoreNamespace, Identifier, Arg.Any<string>());
        }
        _audit.InformationEntries.ShouldEqual(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_let_the_legacy_rpc_succeed_when_coverage_is_complete(bool allowNew)
    {
        if (allowNew) await _service.AllowNewEncryptionKey(new AllowNewEncryptionKeyRequest { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context);
        else await _service.DeleteEncryptionKey(new DeleteEncryptionKeyRequest { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService;

public class when_destructive_operations_are_not_enforced : given.a_compliance_boundary
{
    void Establish() => Configure(enforce: false);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_keep_the_audited_fan_out_for_a_non_administrator(bool allowNew)
    {
        Deny(OtherEventStore);
        Deny(ThirdEventStore);
        (await Operate(allowNew)).ShouldBeEmpty();
        foreach (var store in new[] { EventStore, OtherEventStore, ThirdEventStore })
        {
            if (allowNew) await _keyStore.Received(1).AllowNewKeyFor(store, EventStoreNamespace, Identifier);
            else await _keyStore.Received(1).DeleteFor(store, EventStoreNamespace, Identifier);
        }

        _audit.Entries.ShouldEqual(1);
        string.Join(' ', _audit.Values).ShouldContain("audit");
    }
}

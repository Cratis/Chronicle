// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService.when_defaults_apply;

public class when_called_in_process : given.a_compliance_boundary
{
    void Establish()
    {
        Configure(enforce: false, useDefaults: true);
        _context = default;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_cover_every_store(bool allowNew)
    {
        (await Operate(allowNew)).ShouldBeEmpty();
        foreach (var store in new[] { EventStore, OtherEventStore, ThirdEventStore })
        {
            if (allowNew) await _keyStore.Received(1).AllowNewKeyFor(store, EventStoreNamespace, Identifier);
            else await _keyStore.Received(1).DeleteFor(store, EventStoreNamespace, Identifier);
        }
    }
}

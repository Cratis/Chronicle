// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

public class when_the_query_declares_no_capability : given.a_store_capability_caller
{
    bool _permitted;

    async Task Because() => _permitted = await StoreCapabilityGuard.Permits(_context, new("shop"));

    [Fact] void should_permit_the_query() => _permitted.ShouldBeTrue();
    [Fact] void should_not_ask_for_a_decision() => _asked.ShouldBeEmpty();
}

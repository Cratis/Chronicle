// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_a_command_needs_read_and_append_and_both_are_granted : given.a_command_filter
{
    void Establish()
    {
        _enforce = true;
        Grant("shop", StoreAccess.Read, StoreAccess.Append);
    }

    async Task Because() => _result = await _filter.OnExecution(ContextFor(new ReadAndAppendLike("shop", "tenant")));

    [Fact] void should_not_refuse() => _result.ShouldBeNull();
}

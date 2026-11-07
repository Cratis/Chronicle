// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_a_command_targets_another_store_with_read_on_the_source_and_append_and_manage_on_the_target : given.a_command_filter
{
    void Establish()
    {
        _enforce = true;
        Grant("source", StoreAccess.Read);
        Grant("target", StoreAccess.Append, StoreAccess.Manage);
    }

    async Task Because() => _result = await _filter.OnExecution(ContextFor(new CopyAcrossStores("source", "tenant", "target")));

    [Fact] void should_not_refuse() => _result.ShouldBeNull();
}

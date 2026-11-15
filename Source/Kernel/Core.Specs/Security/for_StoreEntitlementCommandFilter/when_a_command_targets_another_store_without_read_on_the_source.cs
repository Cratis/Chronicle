// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_a_command_targets_another_store_without_read_on_the_source : given.a_command_filter
{
    void Establish()
    {
        _enforce = true;
        Grant("target", StoreAccess.Append, StoreAccess.Manage);
    }

    async Task Because() => _result = await _filter.OnExecution(ContextFor(new CopyAcrossStores("source", "tenant", "target")));

    [Fact] void should_refuse() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_use_the_common_message() => _result.AuthorizationFailureReason.ShouldEqual(StoreEntitlementCommandFilter.Refused);
}

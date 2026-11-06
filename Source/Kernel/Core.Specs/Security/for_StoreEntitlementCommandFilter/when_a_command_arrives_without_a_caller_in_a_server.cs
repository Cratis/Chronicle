// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_a_command_arrives_without_a_caller_in_a_server : given.a_command_filter
{
    void Establish()
    {
        _enforce = true;
        _hasHttpContext = false;
    }

    async Task Because() => _result = await _filter.OnExecution(ContextFor(new AppendLike("shop", "tenant")));

    [Fact] void should_refuse() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_use_the_common_message() => _result.AuthorizationFailureReason.ShouldEqual(StoreEntitlementCommandFilter.Refused);
}

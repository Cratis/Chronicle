// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_a_command_arrives_in_process : given.a_command_filter
{
    void Establish()
    {
        _enforce = true;
        _hasHttpContext = false;
        _inProcess = true;
    }

    async Task Because() => _result = await _filter.OnExecution(ContextFor(new AppendLike("shop", "tenant")));

    [Fact] void should_not_refuse() => _result.ShouldBeNull();
    [Fact] void should_not_decide() => _decisions.ReceivedCalls().ShouldBeEmpty();
}

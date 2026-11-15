// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_a_command_has_no_caller : given.a_command_filter
{
    void Establish() => _hasHttpContext = false;

    async Task Because() => _result = await _filter.OnExecution(ContextFor(new AppendLike("shop", "tenant")));

    [Fact] void should_leave_an_audited_command_to_the_pipeline() => _result.ShouldBeNull();
}

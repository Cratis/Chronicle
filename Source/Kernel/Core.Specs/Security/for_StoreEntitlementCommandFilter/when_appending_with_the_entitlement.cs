// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_appending_with_the_entitlement : given.a_command_filter
{
    void Establish()
    {
        _enforce = true;
        Grant("shop", StoreAccess.Append);
    }

    async Task Because() => _result = await _filter.OnExecution(ContextFor(new AppendLike("shop", "tenant")));

    [Fact] void should_not_refuse() => _result.ShouldBeNull();

    [Fact] void should_not_log() => _loggers.Messages.ShouldBeEmpty();
}

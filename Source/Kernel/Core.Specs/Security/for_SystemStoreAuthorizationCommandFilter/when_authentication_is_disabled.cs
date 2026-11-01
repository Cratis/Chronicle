// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_SystemStoreAuthorizationCommandFilter;

public class when_authentication_is_disabled : given.a_command_filter
{
    void Establish()
    {
        _authentication = new Authentication { Enabled = false };
        _caller = CallerNamed(Anonymous);
    }

    async Task Because() => _result = await _filter.OnExecution(ContextFor(Retry("System")));

    [Fact] void should_not_refuse() => _result.ShouldBeNull();
}

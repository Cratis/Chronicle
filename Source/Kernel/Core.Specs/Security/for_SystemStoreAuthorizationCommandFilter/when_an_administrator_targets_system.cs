// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreAuthorizationCommandFilter;

public class when_an_administrator_targets_system : given.a_command_filter
{
    async Task Because() => _result = await _filter.OnExecution(ContextFor(Retry("System")));

    [Fact] void should_not_refuse() => _result.ShouldBeNull();
}

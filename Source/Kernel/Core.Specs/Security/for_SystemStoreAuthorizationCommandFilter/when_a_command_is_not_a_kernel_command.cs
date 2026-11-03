// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreAuthorizationCommandFilter;

public class when_a_command_is_not_a_kernel_command : given.a_command_filter
{
    void Establish() => _caller = CallerNamed(Anonymous);

    async Task Because() => _result = await _filter.OnExecution(ContextFor(new ApplicationCommand("System", "a#b")));

    [Fact] void should_not_give_a_verdict() => _result.ShouldBeNull();
}

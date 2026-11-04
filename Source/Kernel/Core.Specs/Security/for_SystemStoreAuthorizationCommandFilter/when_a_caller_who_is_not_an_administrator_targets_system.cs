// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreAuthorizationCommandFilter;

public class when_a_caller_who_is_not_an_administrator_targets_system : given.a_command_filter
{
    [Theory]
    [MemberData(nameof(NonAdministrators))]
    public async Task should_refuse_the_command(string caller)
    {
        _caller = CallerNamed(caller);
        var result = await _filter.OnExecution(ContextFor(Retry("System")));
        result.IsAuthorized.ShouldBeFalse();
        result.AuthorizationFailureReason.ShouldEqual(SystemStoreAccess.Refused);
    }
}

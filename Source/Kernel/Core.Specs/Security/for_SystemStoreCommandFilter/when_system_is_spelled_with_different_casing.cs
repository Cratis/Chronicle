// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreCommandFilter;

public class when_system_is_spelled_with_different_casing : given.a_command_filter
{
    void Establish() => _caller = CallerNamed(Application);

    [Theory]
    [InlineData("system")]
    [InlineData("SYSTEM")]
    [InlineData("sYsTeM")]
    public async Task should_refuse_the_command(string spelling)
    {
        var result = await _filter.OnExecution(ContextFor(Retry(spelling)));
        result.IsAuthorized.ShouldBeFalse();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.Security.for_SystemStoreCommandFilter;

public class when_a_command_scenario_validates_system : Specification
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_use_the_scenario_caller_without_filter_exceptions(bool isAdministrator)
    {
        await using var scenario = ChronicleCommandScenario.For<Redact>(isAdministrator);
        var result = await scenario.Validate(new Redact("System", "Default", "event-log", 0, "some reason"));

        result.HasExceptions.ShouldBeFalse();
        result.IsAuthorized.ShouldEqual(isAdministrator);
        if (isAdministrator)
        {
            result.IsValid.ShouldBeFalse();
            result.ValidationResults.SelectMany(validation => validation.Members).ShouldContain("eventStore");
        }
        else
        {
            result.AuthorizationFailureReason.ShouldEqual(SystemStoreAccess.Refused);
        }
    }
}

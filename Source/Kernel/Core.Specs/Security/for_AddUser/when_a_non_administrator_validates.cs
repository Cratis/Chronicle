// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Security.for_AddUser;

public class when_a_non_administrator_validates : Specification
{
    readonly CommandScenario<AddUser> _scenario = ChronicleCommandScenario.For<AddUser>(isAdministrator: false);
    CommandResult _result;

    async Task Because() => _result = await _scenario.Validate(new AddUser(Guid.Empty, string.Empty, string.Empty, string.Empty));

    [Fact] void should_reject_the_caller() => _result.ShouldNotBeAuthorized();
    [Fact] void should_resolve_the_policy_without_exceptions() => _result.ShouldNotHaveExceptions();
    [Fact] void should_not_reach_validation() => _result.ValidationResults.ShouldBeEmpty();

    async Task Destroy() => await _scenario.DisposeAsync();
}

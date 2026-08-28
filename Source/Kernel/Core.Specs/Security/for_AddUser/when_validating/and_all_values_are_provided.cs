// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Security.for_AddUser.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<AddUser> _scenario = new();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Validate(new AddUser(Guid.NewGuid(), "some-user", "some.user@some-domain.com", "some-password-123"));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}

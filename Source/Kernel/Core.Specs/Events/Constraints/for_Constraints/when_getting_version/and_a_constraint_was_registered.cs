// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.when_getting_version;

public class and_a_constraint_was_registered : given.a_constraints_system
{
    ConstraintsVersion _before;
    ConstraintsVersion _after;

    async Task Because()
    {
        _before = await _constraints.GetVersion();
        await _constraints.Register(
        [
            new UniqueConstraintDefinition("unique-thing", [new UniqueConstraintEventDefinition("some-event", ["Some"])])
        ]);
        _after = await _constraints.GetVersion();
    }

    [Fact] void should_change_the_version() => _after.ShouldNotEqual(_before);
}

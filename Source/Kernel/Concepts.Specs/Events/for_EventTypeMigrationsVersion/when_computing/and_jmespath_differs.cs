// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeMigrationsVersion.when_computing;

public class and_jmespath_differs : given.migration_definitions
{
    EventTypeMigrationsVersion _first;
    EventTypeMigrationsVersion _second;

    void Because()
    {
        _first = EventTypeMigrationsVersion.For([Definition()]);
        _second = EventTypeMigrationsVersion.For([Definition(upcast: """{"value":"changed"}""")]);
    }

    [Fact] void should_compute_a_different_version() => _second.ShouldNotEqual(_first);
}

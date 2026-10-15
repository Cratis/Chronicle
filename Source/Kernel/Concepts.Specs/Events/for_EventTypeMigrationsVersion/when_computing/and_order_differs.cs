// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeMigrationsVersion.when_computing;

public class and_order_differs : given.migration_definitions
{
    EventTypeMigrationsVersion _first;
    EventTypeMigrationsVersion _second;

    void Because()
    {
        _first = EventTypeMigrationsVersion.For([Definition(1), Definition(2)]);
        _second = EventTypeMigrationsVersion.For([Definition(2), Definition(1)]);
    }

    [Fact] void should_compute_the_same_version() => _second.ShouldEqual(_first);
}

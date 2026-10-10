// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeMigrationsVersion.when_computing;

public class and_there_are_none : given.migration_definitions
{
    EventTypeMigrationsVersion _version;

    void Because()
    {
        _version = EventTypeMigrationsVersion.For([]);
    }

    [Fact] void should_use_none() => _version.ShouldEqual(EventTypeMigrationsVersion.None);
}

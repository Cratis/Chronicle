// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeMigrationsVersion.when_computing;

public class and_operations_differ : given.migration_definitions
{
    EventTypeMigrationDefinition _definition;
    EventTypeMigrationsVersion _version;

    void Establish() => _definition = Definition();
    void Because()
    {
        _version = EventTypeMigrationsVersion.For([_definition with { Operations = [new EventTypeMigrationOperations(EventTypeMigrationOperation.Rename, Substitute.For<IEventTypeMigrationOperationDetails>())] }]);
    }

    [Fact] void should_ignore_descriptive_operations() => _version.ShouldEqual(EventTypeMigrationsVersion.For([_definition]));
}

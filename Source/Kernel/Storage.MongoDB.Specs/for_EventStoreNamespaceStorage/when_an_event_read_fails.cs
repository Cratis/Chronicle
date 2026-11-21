// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreNamespaceStorage;

public class when_an_event_read_fails : given.a_namespace_database
{
    Exception _error;

    void Establish()
    {
        _names.Add(WellKnownCollectionNames.EventLog);
        _readFailure = new OperationCanceledException("Simulated cancellation");
    }

    async Task Because() => _error = await Catch.Exception(_database.HasEvents);

    [Fact] void should_not_turn_a_failed_probe_into_an_empty_namespace() => _error.ShouldBeSame(_readFailure);
    [Fact] void should_dispose_the_catalog_cursor_when_reading_fails() => _cursors.ForEach(_ => _.Received().Dispose());
}

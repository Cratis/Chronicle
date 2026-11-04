// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionHistoryStorage;

public class when_reading_an_unknown_sqlite_projection : given.a_sqlite_history_storage
{
    Task Because() => Read();
    [Fact] void should_return_no_history() => _entries.ShouldBeEmpty();
}

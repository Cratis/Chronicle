// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionHistoryStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_reading_an_unknown_postgresql_projection(PostgreSqlFixture fixture) : given.a_postgresql_history_storage(fixture)
{
    Task Because() => Read();
    [Fact] void should_return_no_history() => _entries.ShouldBeEmpty();
}

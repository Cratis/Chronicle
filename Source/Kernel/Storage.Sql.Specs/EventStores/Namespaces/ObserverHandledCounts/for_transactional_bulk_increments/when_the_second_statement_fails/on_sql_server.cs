// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.when_the_second_statement_fails;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : conformance(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase));

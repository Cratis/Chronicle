// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_rolling_back_on_a_storage_failure;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : conformance(new MembershipHarness(fixture.CreateDatabase, fixture.DropDatabase, true));

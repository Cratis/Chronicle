// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_jobs;

[Collection(PostgreSqlCollection.Name)]
public class and_the_namespace_is_not_bound(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    Exception _error;

    async Task Establish() { await using var scope = await Database.Cluster(); }

    void Because() => _error = Catch.Exception(() => Database.GetJobsDbContextOptions(Store, Namespace));

    [Fact] void should_refuse_to_compute_an_unchecked_name() => _error.ShouldBeOfExactType<DatabaseNameBindingFailure>();
}

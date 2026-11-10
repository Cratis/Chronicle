// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Integration;
using Cratis.Chronicle.Storage.Neo4j;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_creating_the_database.when_the_server_may_not_create_it.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_creating_the_database;

/// <summary>
/// Turning off creating databases leaves the placement to the platform: a missing database fails instead of being created. It has no effect when the namespaces share one database.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class when_the_server_may_not_create_it(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : Neo4jSpecification(fixture)
    {
        public Exception? Error;
        public IReadOnlyList<string> After = default!;
        public string Expected = default!;

        async Task Because()
        {
            Expected = GraphDatabaseName.For(null, EventStore, Namespace);
            var options = new Neo4jOptions
            {
                Server = Server.HostUri,
                UserName = Neo4jGraphServer.UserName,
                PasswordSecret = PasswordKey,
                DatabaseMode = Fixture.DatabaseMode,
                CreateIfMissing = false
            };
            Error = await Catch.Exception(() => CreateSink(null, null, CreateConnections(options)).EnsureSchema());
            After = await Databases();
        }
    }

    [Fact] void should_fail_with_the_error_of_the_server_when_the_database_is_missing() => (Context.Error is not null).ShouldEqual(!Context.IsSingleDatabase);
    [Fact] void should_not_create_the_database() => Context.After.Contains(Context.Expected).ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Integration;
using Cratis.Chronicle.Storage.Neo4j;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_creating_the_database.with_a_prefix.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_creating_the_database;

/// <summary>
/// A prefix names the databases of the namespaces, so one server can be shared between applications. It has no meaning when the namespaces share one database.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_a_prefix(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : Neo4jSpecification(fixture)
    {
        public IReadOnlyList<string> Databases_ = default!;
        public string Expected = default!;
        public string Unprefixed = default!;

        async Task Because()
        {
            Expected = GraphDatabaseName.For("pfx", EventStore, Namespace);
            Unprefixed = GraphDatabaseName.For(null, EventStore, Namespace);
            TrackDatabase(Expected);
            var options = new Neo4jOptions
            {
                Server = Server.HostUri,
                UserName = Neo4jGraphServer.UserName,
                PasswordSecret = PasswordKey,
                DatabaseMode = Fixture.DatabaseMode,
                DatabasePrefix = "pfx"
            };
            await CreateSink(null, null, CreateConnections(options)).EnsureSchema();
            Databases_ = await Databases();
        }
    }

    [Fact] void should_create_the_database_with_the_prefix_when_every_namespace_has_one() => Context.Databases_.Contains(Context.Expected).ShouldEqual(!Context.IsSingleDatabase);
    [Fact] void should_not_create_the_database_without_the_prefix() => Context.Databases_.Contains(Context.Unprefixed).ShouldBeFalse();
    [Fact] void should_start_the_name_with_the_prefix() => Context.Expected.ShouldEqual($"pfx-{Neo4jSpecification.EventStore}-{Context.Namespace}");
}

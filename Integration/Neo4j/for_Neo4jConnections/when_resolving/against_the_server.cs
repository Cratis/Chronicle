// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Neo4j;
using Neo4j.Driver;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jConnections.when_resolving.against_the_server.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jConnections.when_resolving;

/// <summary>
/// Connections resolve the database of an event store and a namespace as the database mode places it, and authenticate with the password the configuration key holds.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class against_the_server(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : Neo4jSpecification(fixture)
    {
        public Neo4jConnection Runtime = default!;
        public Neo4jConnection Schema = default!;
        public Neo4jConnection OtherNamespace = default!;
        public string Answer = default!;

        async Task Because()
        {
            Runtime = await Connections.Resolve(EventStore, Namespace, ConnectionPurpose.Runtime);
            Schema = await Connections.Resolve(EventStore, Namespace, ConnectionPurpose.Schema);
            OtherNamespace = await Connections.Resolve(EventStore, NewNamespace(), ConnectionPurpose.Runtime);
            await using var session = Runtime.OpenSystemSession();
            var cursor = await session.RunAsync("SHOW DATABASES YIELD name WHERE name = 'system' RETURN name AS answer");
            Answer = (await cursor.SingleAsync())["answer"].As<string>();
        }
    }

    [Fact] void should_name_the_database_as_the_database_mode_places_it() => Context.Runtime.Database.ShouldEqual(Context.IsSingleDatabase ? "neo4j" : $"{Neo4jSpecification.EventStore}-{Context.Namespace}");
    [Fact] void should_only_create_the_database_when_every_namespace_has_one() => Context.Runtime.CanCreateDatabase.ShouldEqual(!Context.IsSingleDatabase);
    [Fact] void should_resolve_the_same_database_for_the_schema() => Context.Schema.Database.ShouldEqual(Context.Runtime.Database);
    [Fact] void should_resolve_another_database_for_another_namespace_when_every_namespace_has_one() => (Context.OtherNamespace.Database == Context.Runtime.Database).ShouldEqual(Context.IsSingleDatabase);
    [Fact] void should_share_the_driver_between_namespaces() => ReferenceEquals(Context.OtherNamespace.Driver, Context.Runtime.Driver).ShouldBeTrue();
    [Fact] void should_authenticate_with_the_password_read_from_the_configuration_key() => Context.Answer.ShouldEqual("system");
}

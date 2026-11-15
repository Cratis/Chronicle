// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Neo4j;
using context = Cratis.Chronicle.Integration.GraphProjections.when_resolving_the_database.for_namespaces.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_resolving_the_database;

/// <summary>
/// A server with one database cannot hold a database per namespace, so what the sink asks for is checked on the connection it resolves,
/// which is what the sink opens its sessions on, and never on the server.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(GraphChronicleCollection.Name)]
public class for_namespaces(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : GraphSpecification(fixture)
    {
        public Dictionary<string, string> Databases = [];

        async Task Because()
        {
            var connections = Services.GetRequiredService<INeo4jConnections>();
            foreach (var @namespace in new[] { "Default", "tenant-a", "Acme Corp" })
            {
                var connection = await connections.Resolve(Constants.EventStore, @namespace, ConnectionPurpose.Runtime);
                Databases[@namespace] = connection.Database;
            }
        }
    }

    [Fact] void should_use_the_configured_mode() => Context.Databases.Count.ShouldEqual(3);
    [Fact] void should_name_the_database_of_the_default_namespace() => Context.Databases["Default"].ShouldEqual(Expected("Default"));
    [Fact] void should_name_the_database_of_a_namespace() => Context.Databases["tenant-a"].ShouldEqual(Expected("tenant-a"));
    [Fact] void should_name_the_database_of_a_namespace_that_needs_sanitizing() => Context.Databases["Acme Corp"].ShouldEqual(Expected("Acme Corp"));

    string Expected(string @namespace) => Context.IsSingleDatabase ? Neo4jGraphServer.Database : GraphDatabaseName.For(null, Constants.EventStore, @namespace);
}

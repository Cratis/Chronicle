// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.when_resolving;

public class and_a_neo4j_sink_server_is_in_a_file : given.connection_files
{
    const string Server = "bolt://graph-from-file:7687";
    Neo4jOptions _neo4j;

    void Establish()
    {
        Setting("Sinks:Graphs", "people");
        Setting("Sinks:Configurations:people:Type", "Neo4j");
        Setting("Sinks:Configurations:people:ServerFile", WriteFile("people-server", $"{Server}\n"));
    }

    void Because()
    {
        AddConfiguration();
        _neo4j = Options().Sinks.Configurations["people"].ToNeo4jOptions();
    }

    [Fact] void should_use_the_server_from_the_file() => _neo4j.Server.ShouldEqual(Server);
}

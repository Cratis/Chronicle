// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.when_resolving;

public class and_a_neo4j_sink_server_and_its_file_are_both_set : given.connection_files
{
    const string FileContents = "bolt://graph-from-file:7687";
    Exception _error;

    void Establish()
    {
        Setting("Sinks:Configurations:people:Type", "Neo4j");
        Setting("Sinks:Configurations:people:Server", "bolt://neo4j:7687");
        Setting("Sinks:Configurations:people:ServerFile", WriteFile("people-server", FileContents));
    }

    void Because() => _error = Catch.Exception(AddConfiguration);

    [Fact] void should_stop_startup() => _error.ShouldBeOfExactType<InvalidConnectionFile>();
    [Fact] void should_name_the_setting() => _error.Message.ShouldContain("'sinks.configurations.people.server'");
    [Fact] void should_name_the_file_setting() => _error.Message.ShouldContain("'sinks.configurations.people.serverFile'");
    [Fact] void should_not_reveal_the_contents_of_the_file() => _error.Message.ShouldNotContain(FileContents);
}

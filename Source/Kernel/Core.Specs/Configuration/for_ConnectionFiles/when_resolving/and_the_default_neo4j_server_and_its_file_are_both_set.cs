// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.when_resolving;

public class and_the_default_neo4j_server_and_its_file_are_both_set : given.connection_files
{
    const string FileContents = "bolt://default-graph-from-file:7687";
    Exception _error;

    void Establish()
    {
        Setting("Neo4j:Server", "bolt://neo4j:7687");
        Setting("Neo4j:ServerFile", WriteFile("neo4j-server", FileContents));
    }

    void Because() => _error = Catch.Exception(AddConfiguration);

    [Fact] void should_stop_startup() => _error.ShouldBeOfExactType<InvalidConnectionFile>();
    [Fact] void should_name_the_setting() => _error.Message.ShouldContain("'neo4j.server'");
    [Fact] void should_name_the_file_setting() => _error.Message.ShouldContain("'neo4j.serverFile'");
    [Fact] void should_not_reveal_the_contents_of_the_file() => _error.Message.ShouldNotContain(FileContents);
}

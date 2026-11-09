// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.when_resolving;

public class and_a_password_secret_and_its_file_are_both_set : given.connection_files
{
    Exception _error;

    void Establish()
    {
        Setting("Sinks:Configurations:people:Type", "Neo4j");
        Setting("Sinks:Configurations:people:Server", "bolt://neo4j:7687");
        Setting("Sinks:Configurations:people:PasswordSecret", "NEO4J_PASSWORD");
        Setting("Sinks:Configurations:people:PasswordFile", WriteFile("people-password", "the-graph-password"));
    }

    void Because() => _error = Catch.Exception(AddConfiguration);

    [Fact] void should_stop_startup() => _error.ShouldBeOfExactType<InvalidConnectionFile>();
    [Fact] void should_name_the_password_secret() => _error.Message.ShouldContain("'sinks.configurations.people.passwordSecret'");
    [Fact] void should_name_the_password_file() => _error.Message.ShouldContain("'sinks.configurations.people.passwordFile'");
}

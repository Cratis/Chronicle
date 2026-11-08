// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.when_resolving;

public class and_a_neo4j_password_is_in_a_file : given.connection_files
{
    const string Password = "the-graph-password";
    Neo4jOptions _neo4j;

    void Establish()
    {
        Setting("Sinks:Graphs", "people");
        Setting("Sinks:Configurations:people:Type", "Neo4j");
        Setting("Sinks:Configurations:people:Server", "bolt://neo4j:7687");
        Setting("Sinks:Configurations:people:UserName", "neo4j");
        Setting("Sinks:Configurations:people:PasswordFile", WriteFile("people-password", $"{Password}\n"));
    }

    void Because()
    {
        AddConfiguration();
        _neo4j = Options().Sinks.Configurations["people"].ToNeo4jOptions();
    }

    [Fact] void should_point_the_password_secret_at_the_password() => _configuration[_neo4j.PasswordSecret!].ShouldEqual(Password);
    [Fact] void should_not_put_the_password_in_the_options() => _neo4j.PasswordSecret.ShouldNotEqual(Password);
}

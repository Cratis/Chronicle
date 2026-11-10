// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.when_resolving;

public class and_the_default_neo4j_connection_is_in_files : given.connection_files
{
    const string Server = "bolt://default-graph-from-file:7687";
    const string Password = "the-default-graph-password";
    Neo4jOptions _neo4j;

    void Establish()
    {
        Setting("Neo4j:ServerFile", WriteFile("neo4j-server", $"{Server}\n"));
        Setting("Neo4j:UserName", "neo4j");
        Setting("Neo4j:PasswordFile", WriteFile("neo4j-password", $"{Password}\n"));
    }

    void Because()
    {
        AddConfiguration();
        _neo4j = Options().Neo4j;
    }

    [Fact] void should_use_the_server_from_the_file() => _neo4j.Server.ShouldEqual(Server);
    [Fact] void should_point_the_password_secret_at_the_password() => _configuration[_neo4j.PasswordSecret!].ShouldEqual(Password);
    [Fact] void should_not_put_the_password_in_the_options() => _neo4j.PasswordSecret.ShouldNotEqual(Password);
}

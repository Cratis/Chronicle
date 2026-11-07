// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_Neo4jOptions.when_binding_configuration;

public class and_neo4j_is_configured : Specification
{
    ChronicleOptions _options;
    ChronicleOptions _defaults;

    void Because()
    {
        _options = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["neo4j:server"] = "bolt://graph.example:7687",
            ["neo4j:userName"] = "chronicle",
            ["neo4j:passwordSecret"] = "NEO4J_PASSWORD",
            ["neo4j:databasePrefix"] = "cratis",
            ["neo4j:databaseMode"] = "Single",
            ["neo4j:singleDatabase"] = "graph",
            ["neo4j:createIfMissing"] = "false"
        }).Build().Get<ChronicleOptions>()!;
        _defaults = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build().Get<ChronicleOptions>() ?? new();
    }

    [Fact] void should_bind_the_server() => _options.Neo4j.Server.ShouldEqual("bolt://graph.example:7687");
    [Fact] void should_bind_the_user_name() => _options.Neo4j.UserName.ShouldEqual("chronicle");
    [Fact] void should_bind_the_password_secret_reference() => _options.Neo4j.PasswordSecret.ShouldEqual("NEO4J_PASSWORD");
    [Fact] void should_bind_the_database_prefix() => _options.Neo4j.DatabasePrefix.ShouldEqual("cratis");
    [Fact] void should_bind_the_database_mode() => _options.Neo4j.DatabaseMode.ShouldEqual(Neo4jDatabaseMode.Single);
    [Fact] void should_bind_the_single_database() => _options.Neo4j.SingleDatabase.ShouldEqual("graph");
    [Fact] void should_bind_create_if_missing() => _options.Neo4j.CreateIfMissing.ShouldBeFalse();
    [Fact] void should_default_to_a_database_per_namespace() => _defaults.Neo4j.DatabaseMode.ShouldEqual(Neo4jDatabaseMode.PerNamespace);
    [Fact] void should_default_to_creating_missing_databases() => _defaults.Neo4j.CreateIfMissing.ShouldBeTrue();
    [Fact] void should_default_to_the_neo4j_database_in_single_mode() => _defaults.Neo4j.SingleDatabase.ShouldEqual("neo4j");
}

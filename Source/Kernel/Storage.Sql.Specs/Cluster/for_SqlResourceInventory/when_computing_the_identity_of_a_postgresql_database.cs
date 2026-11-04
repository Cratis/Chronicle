// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Connections;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlResourceInventory;

public class when_computing_the_identity_of_a_postgresql_database : Specification
{
    static readonly IReadOnlyDictionary<string, string> _none = new Dictionary<string, string>();

    string _first;
    string _otherSchema;
    string _otherCase;
    string _otherServer;

    void Because()
    {
        _first = SqlResourceInventory.IdentityOf(Target("Host=DB1;Database=shop+es+a", "public"), _none);
        _otherSchema = SqlResourceInventory.IdentityOf(Target("Host=DB1;Database=shop+es+a", "tenant"), _none);
        _otherCase = SqlResourceInventory.IdentityOf(Target("Host=DB1;Database=SHOP+es+a", "public"), _none);
        _otherServer = SqlResourceInventory.IdentityOf(Target("Host=db2;Database=shop+es+a", "public"), _none);
    }

    [Fact] void should_not_overlap_when_the_schemas_differ() => _otherSchema.ShouldNotEqual(_first);
    [Fact] void should_compare_names_by_ordinal() => _otherCase.ShouldNotEqual(_first);
    [Fact] void should_not_overlap_when_the_servers_differ() => _otherServer.ShouldNotEqual(_first);
    [Fact] void should_ignore_the_case_of_the_host() => SqlResourceInventory.IdentityOf(Target("Host=db1;Database=shop+es+a", "public"), _none).ShouldEqual(_first);

    static SqlTarget Target(string connectionString, string schema) => new(connectionString, schema, null);
}

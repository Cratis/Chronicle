// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Connections;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlResourceInventory;

public class when_computing_the_identity_of_a_sql_server_database : Specification
{
    string _first;
    string _second;
    string _unlisted;

    void Because()
    {
        var representatives = new Dictionary<string, string> { ["Caf\u00e9"] = "Cafe", ["Cafe"] = "Cafe" };
        _first = SqlResourceInventory.IdentityOf(Target("Cafe"), representatives);
        _second = SqlResourceInventory.IdentityOf(Target("Caf\u00e9"), representatives);
        _unlisted = SqlResourceInventory.IdentityOf(Target("Other"), representatives);
    }

    [Fact] void should_use_the_representative_the_server_gives_the_name() => _second.ShouldEqual(_first);
    [Fact] void should_keep_names_the_server_does_not_group() => _unlisted.ShouldNotEqual(_first);

    static SqlTarget Target(string database) => new($"Server=SQL1;Initial Catalog={database};", "dbo", null);
}

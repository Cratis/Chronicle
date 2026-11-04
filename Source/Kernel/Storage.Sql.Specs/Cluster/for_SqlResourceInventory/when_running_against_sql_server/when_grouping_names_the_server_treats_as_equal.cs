// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlResourceInventory.when_running_against_sql_server;

[Collection(SqlServerCollection.Name)]
public class when_grouping_names_the_server_treats_as_equal(SqlServerFixture fixture) : Specification
{
    static readonly (string Left, string Right)[] _pairs =
    [
        ("Orders", "ORDERS"),
        ("Caf\u00e9", "Cafe"),
        ("Caf\u00e9", "CAFE"),
        ("Shop", "\uff33\uff48\uff4f\uff50"),
        ("\u30ab\u30ca", "\u304b\u306a"),
        ("Orders", "Orders  "),
        ("Orders", "Customers")
    ];

    string _connectionString;
    readonly List<string> _disagreements = [];

    async Task Establish() => _connectionString = await fixture.CreateDatabase();

    async Task Because()
    {
        var catalog = new SqlDatabaseCatalog(_connectionString);
        foreach (var (left, right) in _pairs)
        {
            var groups = await catalog.Groups([left, right]);
            var grouped = groups[left] == groups[right];

            // The server's own equality is the reference.
            if (grouped != await catalog.Equal(left, right))
            {
                _disagreements.Add($"{left}|{right}");
            }
        }
    }

    async Task Destroy() => await fixture.DropDatabase(_connectionString);

    [Fact] void should_group_exactly_what_the_server_considers_equal() => _disagreements.ShouldBeEmpty();
}

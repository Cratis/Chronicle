// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory.when_running_against_mongodb;

[Collection(MongoDBCollection.Name)]
public class when_the_server_refuses_a_name_as_a_duplicate(MongoDBFixture fixture) : given.a_mongodb_server_with_an_inventory(fixture)
{
    static readonly (string First, string Second)[] _pairs =
    [
        ("shop", "SHOP"),
        ("Shop", "sHoP"),
        ("stra\u00dfe", "STRASSE"),
        ("\u0130stanbul", "istanbul"),
        ("\u03c3igma", "\u03a3IGMA"),
        ("\u00e9cole", "\u00c9COLE"),
        ("k\u212ay", "key")
    ];

    readonly List<string> _refusedButNotGrouped = [];

    async Task Because()
    {
        // The inventory must group at least every pair the server refuses; grouping more than the server does is allowed.
        foreach (var (first, second) in _pairs)
        {
            await TryCreate(first);
            if (await TryCreate(second) is null)
            {
                continue;
            }

            var uses = await Read(first, second);
            if (!IdentitiesOf(uses, first).SequenceEqual(IdentitiesOf(uses, second)))
            {
                _refusedButNotGrouped.Add($"{first}/{second}");
            }
        }
    }

    [Fact] void should_group_every_pair_the_server_refuses() => _refusedButNotGrouped.ShouldBeEmpty();
}

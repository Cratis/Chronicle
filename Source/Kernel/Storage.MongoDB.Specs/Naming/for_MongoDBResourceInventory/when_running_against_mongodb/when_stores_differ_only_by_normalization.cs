// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory.when_running_against_mongodb;

[Collection(MongoDBCollection.Name)]
public class when_stores_differ_only_by_normalization(MongoDBFixture fixture) : given.a_mongodb_server_with_an_inventory(fixture)
{
    Exception? _composed;
    Exception? _decomposed;
    string[] _databases;
    IReadOnlyList<ResourceUse> _uses;

    async Task Because()
    {
        _composed = await TryCreate("caf\u00e9");
        _decomposed = await TryCreate("cafe\u0301");
        _databases = await Databases();
        _uses = await Read("caf\u00e9", "cafe\u0301");
    }

    [Fact] void should_accept_both_on_the_server() => (_composed, _decomposed).ShouldEqual((null, null));
    [Fact] void should_have_two_databases() => _databases.Length.ShouldEqual(2);
    [Fact] void should_keep_them_distinct() => IdentitiesOf(_uses, "caf\u00e9").ShouldNotEqual(IdentitiesOf(_uses, "cafe\u0301"));
}

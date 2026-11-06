// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNameCasing.when_resolving;

/// <summary>
/// A name with no case variant on the server is used as it is requested. The first casing asked for is the one every
/// later case variant opens, so two variants asked for before the database exists cannot create two databases.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class a_name_that_does_not_exist(MongoDBFixture fixture) : given.a_server_with_a_database(fixture)
{
    string _first;
    string _second;

    void Because()
    {
        _first = DatabaseNameCasing.Resolve(_client, $"{_prefix}Studio+es+Fresh");
        _second = DatabaseNameCasing.Resolve(_client, $"{_prefix}Studio+es+fresh");
    }

    [Fact] void should_resolve_to_the_requested_name() => _first.ShouldEqual($"{_prefix}Studio+es+Fresh");
    [Fact] void should_resolve_a_later_case_variant_to_the_first_casing() => _second.ShouldEqual($"{_prefix}Studio+es+Fresh");
}

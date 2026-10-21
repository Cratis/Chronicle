// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Seeding;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Seeding.for_SeededEventEntry;

public class when_roundtripping_an_owned_declaration : Specification
{
    SeededEventEntry _result;

    void Because()
    {
        var entry = new SeededEventEntry("source", "type", "{}", []) { Origin = SeedOrigin.Declared, Owner = "program" };
        _result = BsonSerializer.Deserialize<SeededEventEntry>(entry.ToBson());
    }

    [Fact] void should_preserve_origin() => _result.Origin.ShouldEqual(SeedOrigin.Declared);
    [Fact] void should_preserve_owner() => _result.Owner.ShouldEqual("program");
}

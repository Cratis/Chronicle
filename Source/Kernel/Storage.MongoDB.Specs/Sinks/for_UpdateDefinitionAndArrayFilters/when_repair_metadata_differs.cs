// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_UpdateDefinitionAndArrayFilters;

public class when_repair_metadata_differs : Specification
{
    UpdateDefinitionAndArrayFilters _first;
    UpdateDefinitionAndArrayFilters _second;

    void Establish()
    {
        var update = Builders<BsonDocument>.Update.Set("info.name", "Again");
        BsonDocumentArrayFilterDefinition<BsonDocument>[] filters = [];
        _first = new(update, filters, true) { NullParentPaths = ["info"] };
        _second = new(update, filters, true) { NullArrayParents = [new NullArrayParent("items.$[items].info", [])] };
    }

    [Fact] void should_preserve_positional_record_equality() => _first.Equals(_second).ShouldBeTrue();
    [Fact] void should_preserve_positional_record_hash_code() => _first.GetHashCode().ShouldEqual(_second.GetHashCode());
}

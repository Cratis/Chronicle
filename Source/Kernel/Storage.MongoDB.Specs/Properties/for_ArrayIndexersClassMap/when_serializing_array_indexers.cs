// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Properties.for_ArrayIndexersClassMap;

public class when_serializing_array_indexers : Specification
{
    BsonDocument _document;

    void Because()
    {
        var classMap = new BsonClassMap<ArrayIndexers>();
        new ArrayIndexersClassMap().Configure(classMap);
        classMap.Freeze();
        var serializer = new BsonClassMapSerializer<ArrayIndexers>(classMap);

        _document = [];
        using var writer = new BsonDocumentWriter(_document);
        serializer.Serialize(BsonSerializationContext.CreateRoot(writer), new ArrayIndexers([new ArrayIndexer(new PropertyPath("lines"), new PropertyPath("id"), "one")]));
    }

    [Fact] void should_store_the_members_under_camel_case_elements() => _document.Names.ShouldContainOnly("all");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Properties.for_ArrayIndexerClassMap;

public class when_serializing_an_array_indexer : Specification
{
    BsonDocument _document;

    void Because()
    {
        var classMap = new BsonClassMap<ArrayIndexer>();
        new ArrayIndexerClassMap().Configure(classMap);
        classMap.Freeze();
        var serializer = new BsonClassMapSerializer<ArrayIndexer>(classMap);

        _document = [];
        using var writer = new BsonDocumentWriter(_document);
        serializer.Serialize(BsonSerializationContext.CreateRoot(writer), new ArrayIndexer(new PropertyPath("lines"), new PropertyPath("id"), "one"));
    }

    [Fact] void should_store_the_members_under_camel_case_elements() => _document.Names.ShouldContainOnly("arrayProperty", "identifierProperty", "identifier");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Properties.for_PropertyPathClassMap;

public class when_serializing_a_property_path : Specification
{
    BsonDocument _document;
    PropertyPath _restored;

    void Because()
    {
        var classMap = new BsonClassMap<PropertyPath>();
        new PropertyPathClassMap().Configure(classMap);
        classMap.Freeze();
        var serializer = new BsonClassMapSerializer<PropertyPath>(classMap);

        _document = [];
        using (var writer = new BsonDocumentWriter(_document))
        {
            serializer.Serialize(BsonSerializationContext.CreateRoot(writer), new PropertyPath("customer.name"));
        }

        using var reader = new BsonDocumentReader(_document);
        _restored = serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader));
    }

    [Fact] void should_store_the_path_under_a_camel_case_element() => _document.Names.ShouldContainOnly("path");
    [Fact] void should_store_the_rendered_path() => _document["path"].AsString.ShouldEqual("customer.name");
    [Fact] void should_restore_the_path() => _restored.Path.ShouldEqual("customer.name");
}

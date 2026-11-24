// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Changes.for_ChildrenOrderingClassMap;

public class when_serializing_a_children_ordering : Specification
{
    BsonDocument _document;

    void Because()
    {
        var classMap = new BsonClassMap<ChildrenOrdering>();
        new ChildrenOrderingClassMap().Configure(classMap);
        classMap.Freeze();
        var serializer = new BsonClassMapSerializer<ChildrenOrdering>(classMap);

        _document = [];
        using var writer = new BsonDocumentWriter(_document);
        serializer.Serialize(BsonSerializationContext.CreateRoot(writer), new ChildrenOrdering(ChildrenPlacement.Sorted, new PropertyPath("name"), SortDirection.Descending));
    }

    [Fact] void should_store_the_members_under_camel_case_elements() => _document.Names.ShouldContainOnly("placement", "sortBy", "direction");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.given;

public class a_constraint_definition_serializer : Specification
{
    protected ConstraintDefinitionSerializer _serializer;

    void Establish() => _serializer = new();

    protected IConstraintDefinition Deserialize(BsonDocument document)
    {
        using var stream = new MemoryStream(document.ToBson());
        using var reader = new BsonBinaryReader(stream);
        return _serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), default);
    }
}

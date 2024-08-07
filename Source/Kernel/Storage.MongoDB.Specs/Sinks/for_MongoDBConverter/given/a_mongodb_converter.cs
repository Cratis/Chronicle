// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Schemas;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.given;

public class a_mongodb_converter : Specification
{
    protected MongoDBConverter converter;
    protected Mock<IExpandoObjectConverter> expando_object_converter;
    protected Mock<ITypeFormats> type_formats;
    protected Model model;

    void Establish()
    {
        var generator = new NJsonSchemaGenerator(new SystemTextJsonSchemaGeneratorSettings
        {
            AllowReferencesWithProperties = true,
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }
        });

        expando_object_converter = new();
        type_formats = new();
        model = new Model(nameof(ReadModel), generator.Generate(typeof(ReadModel)));
        converter = new(expando_object_converter.Object, type_formats.Object, model);
    }
}

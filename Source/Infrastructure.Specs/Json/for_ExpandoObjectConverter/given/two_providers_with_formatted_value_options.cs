// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Schemas;
using Cratis.Geospatial;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.given;

public class two_providers_with_formatted_value_options : Specification
{
    protected IExpandoObjectConverter firstConverter;
    protected IExpandoObjectConverter secondConverter;
    protected JsonSchema schema;
    ServiceProvider _firstProvider;
    ServiceProvider _secondProvider;

    void Establish()
    {
        schema = JsonSchema.FromJson(
            """
            {
                "type": "object",
                "properties": {
                    "location": {
                        "type": "object",
                        "properties": {
                            "pointValue": { "type": "object", "format": "point" }
                        }
                    }
                }
            }
            """);
        _firstProvider = CreateProvider(10);
        _secondProvider = CreateProvider(100);
        firstConverter = _firstProvider.GetRequiredService<IExpandoObjectConverter>();
        secondConverter = _secondProvider.GetRequiredService<IExpandoObjectConverter>();
    }

    void Destroy()
    {
        _firstProvider.Dispose();
        _secondProvider.Dispose();
    }

    static ServiceProvider CreateProvider(double coordinateOffset)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new OffsetPointConverter(coordinateOffset));
        return new ServiceCollection()
            .AddSingleton<ITypeFormats, TypeFormats>()
            .AddSingleton(options)
            .AddTransient<IExpandoObjectConverter, ExpandoObjectConverter>()
            .BuildServiceProvider();
    }

    sealed class OffsetPointConverter(double coordinateOffset) : JsonConverter<Point>
    {
        public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var coordinates = document.RootElement.GetProperty("coordinates");
            return new Point(coordinates[0].GetDouble() - coordinateOffset, coordinates[1].GetDouble() - coordinateOffset);
        }

        public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "Point");
            writer.WriteStartArray("coordinates");
            writer.WriteNumberValue(value.Longitude + coordinateOffset);
            writer.WriteNumberValue(value.Latitude + coordinateOffset);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}

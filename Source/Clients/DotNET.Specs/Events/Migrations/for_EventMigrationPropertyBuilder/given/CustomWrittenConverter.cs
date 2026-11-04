// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;

public class CustomWrittenConverter : JsonConverter<CustomWritten>
{
    public override CustomWritten Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, CustomWritten value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Fixed_Name", value.Value);
        writer.WriteEndObject();
    }
}

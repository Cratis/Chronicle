// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Json.for_TimeOnlyJsonConverter;

public class when_converting_from_time_only : Specification
{
    TimeOnlyJsonConverter converter;
    MemoryStream stream;
    TimeOnly input;
    Utf8JsonWriter writer;
    string result;

    void Establish()
    {
        converter = new();
        stream = new();
        input = TimeOnly.FromDateTime(DateTime.UtcNow);
        writer = new(stream);
    }

    void Because()
    {
        converter.Write(writer, input, default);
        writer.Flush();
        result = Encoding.UTF8.GetString(stream.ToArray());
    }

    [Fact] void should_convert_to_correct_date_string() => result.ShouldEqual($"\"{input:O}\"");
}

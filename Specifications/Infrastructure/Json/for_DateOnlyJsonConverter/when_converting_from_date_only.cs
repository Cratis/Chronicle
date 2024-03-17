// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Json.for_DateOnlyJsonConverter;

public class when_converting_from_date_only : Specification
{
    DateOnlyJsonConverter converter;
    MemoryStream stream;
    DateOnly input;
    Utf8JsonWriter writer;
    string result;

    void Establish()
    {
        converter = new();
        stream = new();
        input = DateOnly.FromDateTime(DateTime.UtcNow);
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

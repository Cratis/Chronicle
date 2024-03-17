// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Json.for_TimeOnlyJsonConverter;

public class when_converting_to_time_only : Specification
{
    TimeOnlyJsonConverter converter;
    TimeOnly input;
    TimeOnly result;

    void Establish()
    {
        converter = new();
        input = TimeOnly.FromDateTime(DateTime.UtcNow);
    }

    void Because()
    {
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes($"\"{input:O}\"").AsSpan());
        reader.Read();  // Skip quote
        result = converter.Read(ref reader, typeof(TimeOnly), default);
    }

    [Fact] void should_convert_to_correct_date_only() => result.ShouldEqual(input);
}

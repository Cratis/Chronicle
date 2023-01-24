// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Aksio.Cratis.Json.for_DateOnlyJsonConverter;

public class when_converting_to_date_only_from_string : Specification
{
    DateOnlyJsonConverter converter;
    DateTime now;
    DateOnly input;
    string inputAsString;
    DateOnly result;

    void Establish()
    {
        now = DateTime.Now;
        converter = new();
        input = DateOnly.FromDateTime(now);
        inputAsString = now.ToString();
    }

    void Because()
    {
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes($"\"{inputAsString}\"").AsSpan());
        reader.Read();  // Skip quote
        result = converter.Read(ref reader, typeof(DateOnly), default);
    }

    [Fact] void should_convert_to_correct_date_only() => result.ShouldEqual(input);
}
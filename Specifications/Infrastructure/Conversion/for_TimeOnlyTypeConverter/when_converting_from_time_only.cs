// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Conversion.for_TimeOnlyJsonConverter;

public class when_converting_from_time_only : Specification
{
    TimeOnlyTypeConverter converter;
    TimeOnly input;
    string result;

    void Establish()
    {
        converter = new();
        input = TimeOnly.FromDateTime(DateTime.UtcNow);
    }

    void Because() => result = converter.ToString(input);

    [Fact] void should_convert_to_correct_string() => result.ShouldEqual(input.ToString("O"));
}

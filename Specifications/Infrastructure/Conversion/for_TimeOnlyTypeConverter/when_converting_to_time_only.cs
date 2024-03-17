// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Conversion.for_TimeOnlyJsonConverter;

public class when_converting_to_Time_only : Specification
{
    TimeOnlyTypeConverter converter;
    TimeOnly input;
    TimeOnly result;

    void Establish()
    {
        converter = new();
        input = TimeOnly.FromDateTime(DateTime.UtcNow);
    }

    void Because() => result = converter.Parse(input.ToString("O"));

    [Fact] void should_convert_to_correct_time_only() => result.ShouldEqual(input);
}

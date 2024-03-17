// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Conversion.for_DateOnlyJsonConverter;

public class when_converting_to_date_only : Specification
{
    DateOnlyTypeConverter converter;
    DateOnly input;
    DateOnly result;

    void Establish()
    {
        converter = new();
        input = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    void Because() => result = converter.Parse(input.ToString("O"));

    [Fact] void should_convert_to_correct_date_only() => result.ShouldEqual(input);
}

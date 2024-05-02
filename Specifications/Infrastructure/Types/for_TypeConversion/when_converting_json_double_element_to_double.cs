// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Types.for_TypeConversion;

public class when_converting_json_double_element_to_double : Specification
{
    JsonElement input;
    double result;

    void Establish() => input = JsonDocument.Parse("42.0").RootElement;

    void Because() => result = (double)TypeConversion.Convert(typeof(double), input);

    [Fact] void should_convert_correctly() => result.ShouldEqual(42.0);
}

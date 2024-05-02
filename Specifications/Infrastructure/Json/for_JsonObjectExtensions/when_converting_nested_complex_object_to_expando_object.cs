// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Json.for_JsonObjectExtensions;

public class when_converting_nested_complex_object_to_expando_object : Specification
{
    const int first_level_int_value = 42;
    const string first_level_string_value = "Forty two";
    const int second_level_int_value = 43;
    const string second_level_string_value = "Forty three";
    const int third_level_int_value = 44;
    const string third_level_string_value = "Forty four";
    static int[] first_level_int_array = [int.MaxValue, int.MaxValue - 1, int.MaxValue - 2];
    static string[] second_level_string_array = ["Forty five", "Forty six", "Forty seven"];
    JsonObject json_object;
    dynamic result;

    void Establish()
    {
        var the_object = new
        {
            IntValue = first_level_int_value,
            StringValue = first_level_string_value,
            IntArray = first_level_int_array,

            SecondLevel = new
            {
                IntValue = second_level_int_value,
                StringValue = second_level_string_value,
                StringArray = second_level_string_array,

                ThirdLevel = new
                {
                    IntValue = third_level_int_value,
                    StringValue = third_level_string_value
                }
            }
        };

        json_object = JsonSerializer.SerializeToNode(the_object).AsObject();
    }

    void Because() => result = json_object.AsExpandoObject();

    [Fact] void should_have_first_level_int_value() => ((int)result.IntValue).ShouldEqual(first_level_int_value);
    [Fact] void should_have_first_level_string_value() => ((string)result.StringValue).ShouldEqual(first_level_string_value);
    [Fact] void should_have_first_level_int_array() => ((IEnumerable<object>)result.IntArray).Cast<int>().ShouldContainOnly(first_level_int_array);
    [Fact] void should_have_second_level_int_value() => ((int)result.SecondLevel.IntValue).ShouldEqual(second_level_int_value);
    [Fact] void should_have_second_level_string_value() => ((string)result.SecondLevel.StringValue).ShouldEqual(second_level_string_value);
    [Fact] void should_have_second_level_string_array() => ((IEnumerable<object>)result.SecondLevel.StringArray).Cast<string>().ShouldContainOnly(second_level_string_array);
    [Fact] void should_have_third_level_int_value() => ((int)result.SecondLevel.ThirdLevel.IntValue).ShouldEqual(third_level_int_value);
    [Fact] void should_have_third_level_string_value() => ((string)result.SecondLevel.ThirdLevel.StringValue).ShouldEqual(third_level_string_value);
}

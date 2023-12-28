// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Aksio.Cratis.Json.for_ExpandoObjectConverter;

public class when_converting_complex_structure_to_expando_object : given.an_expando_object_converter
{
    JsonObject source;
    JsonObject reference;
    JsonObject complex_type_for_dictionary;
    JsonObject child;
    dynamic result;

    void Establish()
    {
        reference = new JsonObject
        {
            ["intValue"] = 43,
            ["floatValue"] = 43.43,
            ["doubleValue"] = 43.43,
            ["guidValue"] = Guid.Parse("251b9fbe-83d4-4306-9a5d-9d0e7d4dd456")
        };

        child = new JsonObject
        {
            ["intValue"] = 44,
            ["floatValue"] = 44.44,
            ["doubleValue"] = 44.44,
            ["guidValue"] = "251b9fbe-83d4-4306-9a5d-9d0e7d4dd456",
        };

        complex_type_for_dictionary = new JsonObject
        {
            ["intValue"] = 45,
            ["floatValue"] = 45.45,
            ["doubleValue"] = 45.45,
            ["guidValue"] = Guid.Parse("251b9fbe-83d4-4306-9a5d-9d0e7d4dd456")
        };

        source = new JsonObject
        {
            ["intValue"] = 42,
            ["floatValue"] = 42.42,
            ["doubleValue"] = 42.42,
            ["enumValue"] = 2,
            ["nullableEnumValue"] = 1,
            ["nullableEnumValueSetToNull"] = null,
            ["enumAsStringValueRepresentedAsInt"] = 2,
            ["enumAsStringValueWithJsonConverter"] = 2,
            ["enumAsStringValue"] = "Second",
            ["guidValue"] = "251b9fbe-83d4-4306-9a5d-9d0e7d4dd456",
            ["dateTimeValue"] = DateTime.Parse("2022-10-31T14:51:32.8450000Z"),
            ["dateTimeOffsetValue"] = DateTime.Parse("2022-10-31T14:51:32.8450000Z"),
            ["dateOnlyValue"] = DateTime.Parse("2022-10-31T14:51:32.8450000Z"),
            ["timeOnlyValue"] = DateTime.Parse("2022-10-31T14:51:32.8450000Z"),
            ["reference"] = reference,
            ["children"] = new JsonArray(child),
            ["stringArray"] = new JsonArray { "first", "second" },
            ["intArray"] = new JsonArray { 42, 43 },
            ["stringDictionary"] = new JsonObject
            {
                ["first"] = "firstValue",
                ["second"] = "secondValue"
            },
            ["intDictionary"] = new JsonObject
            {
                ["1"] = 42,
                ["2"] = 43
            },
            ["complexTypeDictionary"] = new JsonObject
            {
                ["first"] = complex_type_for_dictionary
            }
        };
    }

    void Because() => result = converter.ToExpandoObject(source, schema);

    [Fact] void should_set_top_level_int_value_to_be_of_int_type() => ((object)result.intValue).ShouldBeOfExactType<int>();
    [Fact] void should_set_top_level_int_value_to_hold_correct_value() => ((int)result.intValue).ShouldEqual(source["intValue"].GetValue<int>());
    [Fact] void should_set_top_level_float_value_to_be_of_float_type() => ((object)result.floatValue).ShouldBeOfExactType<float>();
    [Fact] void should_set_top_level_float_value_to_hold_correct_value() => ((float)result.floatValue).ShouldEqual((float)source["floatValue"].GetValue<double>());
    [Fact] void should_set_top_level_double_value_to_be_of_float_type() => ((object)result.doubleValue).ShouldBeOfExactType<double>();
    [Fact] void should_set_top_level_double_value_to_hold_correct_value() => ((double)result.doubleValue).ShouldEqual(source["doubleValue"].GetValue<double>());
    [Fact] void should_set_top_level_enum_value_to_be_of_int_type() => ((object)result.enumValue).ShouldBeOfExactType<int>();
    [Fact] void should_set_top_level_enum_value_to_hold_correct_value() => ((int)result.enumValue).ShouldEqual(source["enumValue"].GetValue<int>());
    [Fact] void should_set_top_level_nullable_enum_value_to_be_of_int_type() => ((object)result.nullableEnumValue).ShouldBeOfExactType<int>();
    [Fact] void should_set_top_level_nullable_enum_value_to_hold_correct_value() => ((int)result.nullableEnumValue).ShouldEqual(source["nullableEnumValue"].GetValue<int>());
    [Fact] void should_set_top_level_enum_as_string_value_to_be_of_int_type() => ((object)result.enumValue).ShouldBeOfExactType<int>();
    [Fact] void should_set_top_level_enum_as_string_value_to_hold_correct_value() => ((int)result.enumValue).ShouldEqual((int)AnEnumWithStringValues.Second);
    [Fact] void should_set_top_level_enum_as_string_value_represented_as_int_to_be_of_int_type() => ((object)result.enumAsStringValueRepresentedAsInt).ShouldBeOfExactType<int>();
    [Fact] void should_set_top_level_enum_as_string_value_represented_as_int_to_hold_correct_value() => ((int)result.enumAsStringValueRepresentedAsInt).ShouldEqual((int)AnEnumWithStringValues.Second);
    [Fact] void should_set_top_level_enum_as_string_value_with_string_json_converter_represented_as_int_to_be_of_int_type() => ((object)result.enumAsStringValueWithJsonConverter).ShouldBeOfExactType<int>();
    [Fact] void should_set_top_level_enum_as_string_value_with_string_json_converter_represented_as_int_to_hold_correct_value() => ((int)result.enumAsStringValueWithJsonConverter).ShouldEqual((int)AnEnumWithStringValues.Second);
    [Fact] void should_set_top_level_guid_value_to_be_of_guid_type() => ((object)result.guidValue).ShouldBeOfExactType<Guid>();
    [Fact] void should_set_top_level_guid_value_to_hold_correct_value() => ((Guid)result.guidValue).ShouldEqual(Guid.Parse(source["guidValue"].GetValue<string>()));
    [Fact] void should_set_top_level_date_time_value_to_be_of_date_time_type() => ((object)result.dateTimeValue).ShouldBeOfExactType<DateTime>();
    [Fact] void should_set_top_level_date_time_value_to_hold_correct_value() => ((DateTime)result.dateTimeValue).ShouldEqual(source["dateTimeValue"].GetValue<DateTime>());
    [Fact] void should_set_top_level_date_time_offset_value_to_be_of_date_time_offset_type() => ((object)result.dateTimeOffsetValue).ShouldBeOfExactType<DateTimeOffset>();
    [Fact] void should_set_top_level_date_time_offset_value_to_hold_correct_value() => ((DateTimeOffset)result.dateTimeOffsetValue).ShouldEqual(DateTime.SpecifyKind(source["dateTimeOffsetValue"].GetValue<DateTime>(), DateTimeKind.Utc));
    [Fact] void should_set_top_level_date_only_value_to_be_of_date_time_offset_type() => ((object)result.dateOnlyValue).ShouldBeOfExactType<DateOnly>();
    [Fact] void should_set_top_level_date_only_value_to_hold_correct_value() => ((DateOnly)result.dateOnlyValue).ShouldEqual(DateOnly.FromDateTime(source["dateOnlyValue"].GetValue<DateTime>()));
    [Fact] void should_set_top_level_time_only_value_to_be_of_date_time_offset_type() => ((object)result.timeOnlyValue).ShouldBeOfExactType<TimeOnly>();
    [Fact] void should_set_top_level_time_only_value_to_hold_correct_value() => ((TimeOnly)result.timeOnlyValue).ShouldEqual(TimeOnly.FromDateTime(source["timeOnlyValue"].GetValue<DateTime>()));
    [Fact] void should_set_top_level_string_array_first_item() => ((string)result.stringArray[0]).ShouldEqual("first");
    [Fact] void should_set_top_level_string_array_second_item() => ((string)result.stringArray[1]).ShouldEqual("second");
    [Fact] void should_set_top_level_int_array_first_item() => ((int)result.intArray[0]).ShouldEqual(42);
    [Fact] void should_set_top_level_int_array_second_item() => ((int)result.intArray[1]).ShouldEqual(43);
    [Fact] void should_set_top_level_string_dictionary_first_item() => ((string)result.stringDictionary["first"]).ShouldEqual("firstValue");
    [Fact] void should_set_top_level_string_dictionary_second_item() => ((string)result.stringDictionary["second"]).ShouldEqual("secondValue");
    [Fact] void should_set_top_level_int_dictionary_first_item() => ((int)result.intDictionary["1"]).ShouldEqual(42);
    [Fact] void should_set_top_level_int_dictionary_second_item() => ((int)result.intDictionary["2"]).ShouldEqual(43);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_int_value() => ((int)result.complexTypeDictionary["first"].intValue).ShouldEqual(45);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_float_value() => ((float)result.complexTypeDictionary["first"].floatValue).ShouldEqual((float)45.45f);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_double_value() => ((double)result.complexTypeDictionary["first"].doubleValue).ShouldEqual(45.45);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_guid_value() => ((Guid)result.complexTypeDictionary["first"].guidValue).ShouldEqual(Guid.Parse("251b9fbe-83d4-4306-9a5d-9d0e7d4dd456"));

    [Fact] void should_reference_level_int_value_to_be_of_int_type() => ((object)result.reference.intValue).ShouldBeOfExactType<int>();
    [Fact] void should_reference_level_int_value_to_hold_correct_value() => ((int)result.reference.intValue).ShouldEqual(reference["intValue"].GetValue<int>());
    [Fact] void should_reference_level_float_value_to_be_of_float_type() => ((object)result.reference.floatValue).ShouldBeOfExactType<float>();
    [Fact] void should_reference_level_float_value_to_hold_correct_value() => ((float)result.reference.floatValue).ShouldEqual((float)reference["floatValue"].GetValue<double>());
    [Fact] void should_reference_level_double_value_to_be_of_float_type() => ((object)result.reference.doubleValue).ShouldBeOfExactType<double>();
    [Fact] void should_reference_level_double_value_to_hold_correct_value() => ((double)result.reference.doubleValue).ShouldEqual(reference["doubleValue"].GetValue<double>());
    [Fact] void should_reference_level_guid_value_to_be_of_guid_type() => ((object)result.reference.guidValue).ShouldBeOfExactType<Guid>();
    [Fact] void should_reference_level_guid_value_to_hold_correct_value() => ((Guid)result.reference.guidValue).ShouldEqual(reference["guidValue"].GetValue<Guid>());

    [Fact] void should_child_int_value_to_be_of_int_type() => ((object)result.children[0].intValue).ShouldBeOfExactType<int>();
    [Fact] void should_child_int_value_to_hold_correct_value() => ((int)result.children[0].intValue).ShouldEqual(child["intValue"].GetValue<int>());
    [Fact] void should_child_float_value_to_be_of_float_type() => ((object)result.children[0].floatValue).ShouldBeOfExactType<float>();
    [Fact] void should_child_float_value_to_hold_correct_value() => ((float)result.children[0].floatValue).ShouldEqual((float)child["floatValue"].GetValue<double>());
    [Fact] void should_child_double_value_to_be_of_float_type() => ((object)result.children[0].doubleValue).ShouldBeOfExactType<double>();
    [Fact] void should_child_double_value_to_hold_correct_value() => ((double)result.children[0].doubleValue).ShouldEqual(child["doubleValue"].GetValue<double>());
    [Fact] void should_child_guid_value_to_be_of_guid_type() => ((object)result.children[0].guidValue).ShouldBeOfExactType<Guid>();
    [Fact] void should_child_guid_value_to_hold_correct_value() => ((Guid)result.children[0].guidValue).ShouldEqual(Guid.Parse(child["guidValue"].GetValue<string>()));

    [Fact] void should_set_top_level_missing_string_from_source_to_null() => ((object)result.missingStringFromSource).ShouldBeNull();
    [Fact] void should_set_top_level_missing_int_from_source_to_default_value() => ((int)result.missingIntFromSource).ShouldEqual(0);
}

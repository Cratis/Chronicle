// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using System.Text.Json.Nodes;
using Aksio.Json;

namespace Aksio.Cratis.Json.for_ExpandoObjectConverter;

public class when_converting_complex_structure_to_json_object : given.an_expando_object_converter
{
    ExpandoObject source;
    ExpandoObject child;

    dynamic source_dynamic;
    dynamic child_dynamic;

    JsonObject result;

    void Establish()
    {
        dynamic expando = new ExpandoObject();
        expando.intValue = 42;
        expando.floatValue = 42.42;
        expando.doubleValue = 42.42;
        expando.enumValue = 2;
        expando.nullableEnumValue = 1;
        expando.guidValue = "3023e769-4594-45fd-938e-9b231cf3e3f5";
        expando.dateTimeValue = "2022-10-31T14:51:32.8450000Z";
        expando.dateTimeOffsetValue = "2022-10-31T14:51:32.8450000Z";
        expando.dateOnlyValue = "2022-10-31";
        expando.timeOnlyValue = "2022-10-31T14:51:32";
        expando.reference = new ExpandoObject();
        expando.reference.intValue = 43;
        expando.reference.floatValue = 43.43;
        expando.reference.doubleValue = 43.43;
        expando.reference.guidValue = Guid.Parse("b17a2668-37ba-4a20-ab6a-0aba09926b64");

        dynamic child_expando = new ExpandoObject();
        child_expando.intValue = 44;
        child_expando.floatValue = 44.44;
        child_expando.doubleValue = 44.44;
        child_expando.guidValue = "633f3280-cb69-4a8b-9e03-7fec8c9e7845";

        expando.children = new ExpandoObject[] { child_expando };

        expando.stringDictionary = new Dictionary<string, string>
        {
            { "first", "first value" },
            { "second", "second value" }
        };

        expando.intDictionary = new Dictionary<string, int>
        {
            { "first", 42 },
            { "second", 43 }
        };

        dynamic complexType = new ExpandoObject();
        complexType.intValue = 45;
        complexType.floatValue = 45.45f;
        complexType.doubleValue = 45.45;
        complexType.guidValue = Guid.Parse("a4134076-5823-4189-b017-d82756816305");
        expando.complexTypeDictionary = new Dictionary<string, ExpandoObject>
        {
            { "first", complexType }
        };

        source = expando;
        source_dynamic = expando;

        child = child_expando;
        child_dynamic = child_expando;
    }

    void Because() => result = converter.ToJsonObject(source, schema);

    [Fact] void should_set_top_level_int_value_to_hold_correct_value() => result["intValue"].GetValue<int>().ShouldEqual((int)source_dynamic.intValue);
    [Fact] void should_set_top_level_float_value_to_hold_correct_value() => result["floatValue"].GetValue<float>().ShouldEqual((float)source_dynamic.floatValue);
    [Fact] void should_set_top_level_double_value_to_hold_correct_value() => result["doubleValue"].GetValue<double>().ShouldEqual((double)source_dynamic.doubleValue);
    [Fact] void should_set_top_level_enum_value_to_hold_correct_value() => result["enumValue"].GetValue<int>().ShouldEqual((int)source_dynamic.enumValue);
    [Fact] void should_set_top_level_nullable_enum_value_to_hold_correct_value() => result["nullableEnumValue"].GetValue<int>().ShouldEqual((int)source_dynamic.nullableEnumValue);
    [Fact] void should_set_top_level_guid_value_to_hold_correct_value() => result["guidValue"].GetValue<Guid>().ShouldEqual(Guid.Parse((string)source_dynamic.guidValue));
    [Fact] void should_set_top_level_date_time_value_to_hold_correct_value() => result["dateTimeValue"].GetValue<DateTime>().ShouldEqual(DateTime.Parse((string)source_dynamic.dateTimeValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal));
    [Fact] void should_set_top_level_date_time_offset_value_to_hold_correct_value() => result["dateTimeOffsetValue"].GetValue<DateTimeOffset>().ShouldEqual(DateTimeOffset.Parse((string)source_dynamic.dateTimeOffsetValue));
    [Fact] void should_set_top_level_date_only_value_to_hold_correct_value() => result["dateOnlyValue"].GetValue<DateTime>().ShouldEqual(DateOnly.Parse((string)source_dynamic.dateOnlyValue).ToDateTime(JsonValueExtensions.Noon));
    [Fact] void should_set_top_level_time_only_value_to_hold_correct_value() => result["timeOnlyValue"].GetValue<DateTime>().ShouldEqual(DateTime.MinValue.Add(TimeOnly.FromDateTime(DateTime.Parse((string)source_dynamic.timeOnlyValue)).ToTimeSpan()));
    [Fact] void should_set_top_level_string_dictionary_first_item() => result["stringDictionary"]["first"].GetValue<string>().ShouldEqual((string)source_dynamic.stringDictionary["first"]);
    [Fact] void should_set_top_level_string_dictionary_second_item() => result["stringDictionary"]["second"].GetValue<string>().ShouldEqual((string)source_dynamic.stringDictionary["second"]);
    [Fact] void should_set_top_level_int_dictionary_first_item() => result["intDictionary"]["first"].GetValue<int>().ShouldEqual((int)source_dynamic.intDictionary["first"]);
    [Fact] void should_set_top_level_int_dictionary_second_item() => result["intDictionary"]["second"].GetValue<int>().ShouldEqual((int)source_dynamic.intDictionary["second"]);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_int_value() => result["complexTypeDictionary"]["first"]["intValue"].GetValue<int>().ShouldEqual((int)source_dynamic.complexTypeDictionary["first"].intValue);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_float_value() => result["complexTypeDictionary"]["first"]["floatValue"].GetValue<float>().ShouldEqual((float)source_dynamic.complexTypeDictionary["first"].floatValue);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_double_value() => result["complexTypeDictionary"]["first"]["doubleValue"].GetValue<double>().ShouldEqual((double)source_dynamic.complexTypeDictionary["first"].doubleValue);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_guid_value() => result["complexTypeDictionary"]["first"]["guidValue"].GetValue<Guid>().ShouldEqual((Guid)source_dynamic.complexTypeDictionary["first"].guidValue);


    [Fact] void should_set_reference_object_int_value_to_hold_correct_value() => result["reference"]["intValue"].GetValue<int>().ShouldEqual((int)source_dynamic.reference.intValue);
    [Fact] void should_set_reference_object_float_value_to_hold_correct_value() => result["reference"]["floatValue"].GetValue<float>().ShouldEqual((float)source_dynamic.reference.floatValue);
    [Fact] void should_set_reference_object_double_value_to_hold_correct_value() => result["reference"]["doubleValue"].GetValue<double>().ShouldEqual((double)source_dynamic.reference.doubleValue);
    [Fact] void should_set_reference_object_guid_value_to_hold_correct_value() => result["reference"]["guidValue"].GetValue<Guid>().ShouldEqual((Guid)source_dynamic.reference.guidValue);

    [Fact] void should_set_child_object_int_value_to_hold_correct_value() => result["children"].AsArray()[0]["intValue"].GetValue<int>().ShouldEqual((int)child_dynamic.intValue);
    [Fact] void should_set_child_object_float_value_to_hold_correct_value() => result["children"].AsArray()[0]["floatValue"].GetValue<float>().ShouldEqual((float)child_dynamic.floatValue);
    [Fact] void should_set_child_object_double_value_to_hold_correct_value() => result["children"].AsArray()[0]["doubleValue"].GetValue<double>().ShouldEqual((double)child_dynamic.doubleValue);
    [Fact] void should_set_child_object_guid_value_to_hold_correct_value() => result["children"].AsArray()[0]["guidValue"].GetValue<Guid>().ShouldEqual(Guid.Parse((string)child_dynamic.guidValue));

    [Fact] void should_set_top_level_missing_string_from_source_to_null() => result["missingStringFromSource"].ShouldBeNull();
    [Fact] void should_set_top_level_missing_int_from_source_to_default_value() => result["missingIntFromSource"].GetValue<int>().ShouldEqual(0);
}

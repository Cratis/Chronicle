// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_complex_structure_to_bson_document : given.an_expando_object_converter
{
    ExpandoObject source;
    ExpandoObject child;

    dynamic source_dynamic;
    dynamic child_dynamic;

    BsonDocument result;

    void Establish()
    {
        dynamic expando = new ExpandoObject();
        expando.intValue = 42;
        expando.floatValue = 42.42f;
        expando.doubleValue = 42.42;
        expando.enumValue = 2;
        expando.guidValue = "3023e769-4594-45fd-938e-9b231cf3e3f5";
        expando.dateTimeValue = "2022-10-31T14:51:32.8450000Z";
        expando.dateTimeOffsetValue = "2022-10-31T14:51:32.8450000Z";
        expando.dateOnlyValue = "2022-10-31";
        expando.timeOnlyValue = "2022-10-31T14:51:32";
        expando.reference = new ExpandoObject();
        expando.reference.intValue = 43;
        expando.reference.floatValue = 43.43f;
        expando.reference.doubleValue = 43.43;
        expando.reference.guidValue = Guid.Parse("b17a2668-37ba-4a20-ab6a-0aba09926b64");
        expando.stringDictionary = new Dictionary<string, string>
        {
            { "first", "firstValue" },
            { "second", "secondValue" }
        };
        expando.intDictionary = new Dictionary<string, int>
        {
            { "first", 42 },
            { "second", 43 }
        };

        expando.intChildren = new int[] { 1, 2, 3 };
        expando.stringChildren = new string[] { "first", "second", "third" };

        dynamic child_expando = new ExpandoObject();
        child_expando.intValue = 44;
        child_expando.floatValue = 44.44f;
        child_expando.doubleValue = 44.44;
        child_expando.guidValue = "633f3280-cb69-4a8b-9e03-7fec8c9e7845";

        expando.children = new ExpandoObject[] { child_expando };

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

    void Because() => result = converter.ToBsonDocument(source, schema);

    [Fact] void should_set_top_level_int_value_to_be_of_int_type() => result.GetElement("intValue").Value.ShouldBeOfExactType<BsonInt32>();
    [Fact] void should_set_top_level_int_value_to_hold_correct_value() => result.GetElement("intValue").Value.AsInt32.ShouldEqual((int)source_dynamic.intValue);
    [Fact] void should_set_top_level_float_value_to_be_of_double_type() => result.GetElement("floatValue").Value.ShouldBeOfExactType<BsonDouble>();
    [Fact] void should_set_top_level_float_value_to_hold_correct_value() => result.GetElement("floatValue").Value.AsDouble.ShouldEqual((float)source_dynamic.floatValue);
    [Fact] void should_set_top_level_double_value_to_be_of_double_type() => result.GetElement("doubleValue").Value.ShouldBeOfExactType<BsonDouble>();
    [Fact] void should_set_top_level_double_value_to_hold_correct_value() => result.GetElement("doubleValue").Value.AsDouble.ShouldEqual((double)source_dynamic.doubleValue);
    [Fact] void should_set_top_level_enum_value_to_be_of_double_type() => result.GetElement("enumValue").Value.ShouldBeOfExactType<BsonInt32>();
    [Fact] void should_set_top_level_enum_value_to_hold_correct_value() => result.GetElement("enumValue").Value.AsInt32.ShouldEqual((int)source_dynamic.enumValue);
    [Fact] void should_set_top_level_guid_value_to_be_of_binary_type() => result.GetElement("guidValue").Value.ShouldBeOfExactType<BsonBinaryData>();
    [Fact] void should_set_top_level_guid_value_to_hold_correct_value() => result.GetElement("guidValue").Value.AsGuid.ShouldEqual(Guid.Parse((string)source_dynamic.guidValue));
    [Fact] void should_set_top_level_date_time_value_to_be_of_date_time_type() => result.GetElement("dateTimeValue").Value.ShouldBeOfExactType<BsonDateTime>();
    [Fact] void should_set_top_level_date_time_value_to_hold_correct_value() => result.GetElement("dateTimeValue").Value.ToUniversalTime().ShouldEqual(DateTime.Parse((string)source_dynamic.dateTimeValue).ToUniversalTime());
    [Fact] void should_set_top_level_date_time_offset_value_to_be_of_date_time_type() => result.GetElement("dateTimeOffsetValue").Value.ShouldBeOfExactType<BsonDateTime>();
    [Fact] void should_set_top_level_date_time_offset_value_to_hold_correct_value() => DateTimeOffset.FromUnixTimeMilliseconds(((BsonDateTime)result.GetElement("dateTimeOffsetValue").Value).MillisecondsSinceEpoch).ShouldEqual(DateTimeOffset.Parse((string)source_dynamic.dateTimeOffsetValue));
    [Fact] void should_set_top_level_date_only_value_to_be_of_date_time_type() => result.GetElement("dateOnlyValue").Value.ShouldBeOfExactType<BsonDateTime>();
    [Fact] void should_set_top_level_date_only_value_to_hold_correct_value() => DateOnly.FromDateTime(result.GetElement("dateOnlyValue").Value.ToUniversalTime()).ShouldEqual(DateOnly.Parse((string)source_dynamic.dateOnlyValue));
    [Fact] void should_set_top_level_time_only_value_to_be_of_date_time_type() => result.GetElement("timeOnlyValue").Value.ShouldBeOfExactType<BsonDateTime>();
    [Fact] void should_set_top_level_time_only_value_to_hold_correct_value() => TimeOnly.FromDateTime(result.GetElement("timeOnlyValue").Value.ToUniversalTime()).ShouldEqual(TimeOnly.FromDateTime(DateTime.Parse((string)source_dynamic.timeOnlyValue)));
    [Fact] void should_set_top_level_string_dictionary_first_item() => result.GetElement("stringDictionary").Value.AsBsonDocument.GetElement("first").Value.AsString.ShouldEqual((string)source_dynamic.stringDictionary["first"]);
    [Fact] void should_set_top_level_string_dictionary_second_item() => result.GetElement("stringDictionary").Value.AsBsonDocument.GetElement("second").Value.AsString.ShouldEqual((string)source_dynamic.stringDictionary["second"]);
    [Fact] void should_set_top_level_int_dictionary_first_item() => result.GetElement("intDictionary").Value.AsBsonDocument.GetElement("first").Value.AsInt32.ShouldEqual((int)source_dynamic.intDictionary["first"]);
    [Fact] void should_set_top_level_int_dictionary_second_item() => result.GetElement("intDictionary").Value.AsBsonDocument.GetElement("second").Value.AsInt32.ShouldEqual((int)source_dynamic.intDictionary["second"]);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_int_value() => result.GetElement("complexTypeDictionary").Value.AsBsonDocument.GetElement("first").Value.AsBsonDocument.GetElement("intValue").Value.AsInt32.ShouldEqual((int)source_dynamic.complexTypeDictionary["first"].intValue);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_float_value() => result.GetElement("complexTypeDictionary").Value.AsBsonDocument.GetElement("first").Value.AsBsonDocument.GetElement("floatValue").Value.AsDouble.ShouldEqual((float)source_dynamic.complexTypeDictionary["first"].floatValue);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_double_value() => result.GetElement("complexTypeDictionary").Value.AsBsonDocument.GetElement("first").Value.AsBsonDocument.GetElement("doubleValue").Value.AsDouble.ShouldEqual((double)source_dynamic.complexTypeDictionary["first"].doubleValue);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_guid_value() => result.GetElement("complexTypeDictionary").Value.AsBsonDocument.GetElement("first").Value.AsBsonDocument.GetElement("guidValue").Value.AsGuid.ShouldEqual((Guid)source_dynamic.complexTypeDictionary["first"].guidValue);


    [Fact] void should_reference_object_int_value_to_be_of_int_type() => result.GetElement("reference").Value.AsBsonDocument.GetElement("intValue").Value.ShouldBeOfExactType<BsonInt32>();
    [Fact] void should_reference_object_int_value_to_hold_correct_value() => result.GetElement("reference").Value.AsBsonDocument.GetElement("intValue").Value.AsInt32.ShouldEqual((int)source_dynamic.reference.intValue);
    [Fact] void should_reference_object_float_value_to_be_of_double_type() => result.GetElement("reference").Value.AsBsonDocument.GetElement("floatValue").Value.ShouldBeOfExactType<BsonDouble>();
    [Fact] void should_reference_object_float_value_to_hold_correct_value() => result.GetElement("reference").Value.AsBsonDocument.GetElement("floatValue").Value.AsDouble.ShouldEqual((float)source_dynamic.reference.floatValue);
    [Fact] void should_reference_object_double_value_to_be_of_double_type() => result.GetElement("reference").Value.AsBsonDocument.GetElement("doubleValue").Value.ShouldBeOfExactType<BsonDouble>();
    [Fact] void should_reference_object_double_value_to_hold_correct_value() => result.GetElement("reference").Value.AsBsonDocument.GetElement("doubleValue").Value.AsDouble.ShouldEqual((double)source_dynamic.reference.doubleValue);
    [Fact] void should_reference_object_guid_value_to_be_of_binary_type() => result.GetElement("reference").Value.AsBsonDocument.GetElement("guidValue").Value.ShouldBeOfExactType<BsonBinaryData>();
    [Fact] void should_reference_object_guid_value_to_hold_correct_value() => result.GetElement("reference").Value.AsBsonDocument.GetElement("guidValue").Value.AsGuid.ShouldEqual((Guid)source_dynamic.reference.guidValue);

    [Fact] void should_child_object_int_value_to_be_of_int_type() => result.GetElement("children").Value.AsBsonArray[0].AsBsonDocument.GetElement("intValue").Value.ShouldBeOfExactType<BsonInt32>();
    [Fact] void should_child_object_int_value_to_hold_correct_value() => result.GetElement("children").Value.AsBsonArray[0].AsBsonDocument.GetElement("intValue").Value.AsInt32.ShouldEqual((int)child_dynamic.intValue);
    [Fact] void should_child_object_float_value_to_be_of_double_type() => result.GetElement("children").Value.AsBsonArray[0].AsBsonDocument.GetElement("floatValue").Value.ShouldBeOfExactType<BsonDouble>();
    [Fact] void should_child_object_float_value_to_hold_correct_value() => result.GetElement("children").Value.AsBsonArray[0].AsBsonDocument.GetElement("floatValue").Value.AsDouble.ShouldEqual((float)child_dynamic.floatValue);
    [Fact] void should_child_object_double_value_to_be_of_double_type() => result.GetElement("children").Value.AsBsonArray[0].AsBsonDocument.GetElement("doubleValue").Value.ShouldBeOfExactType<BsonDouble>();
    [Fact] void should_child_object_double_value_to_hold_correct_value() => result.GetElement("children").Value.AsBsonArray[0].AsBsonDocument.GetElement("doubleValue").Value.AsDouble.ShouldEqual((double)child_dynamic.doubleValue);
    [Fact] void should_child_object_guid_value_to_be_of_binary_type() => result.GetElement("children").Value.AsBsonArray[0].AsBsonDocument.GetElement("guidValue").Value.ShouldBeOfExactType<BsonBinaryData>();
    [Fact] void should_child_object_guid_value_to_hold_correct_value() => result.GetElement("children").Value.AsBsonArray[0].AsBsonDocument.GetElement("guidValue").Value.AsGuid.ShouldEqual(Guid.Parse((string)child_dynamic.guidValue));

    [Fact] void should_have_first_integer_child_with_correct_value() => result.GetElement("intChildren").Value.AsBsonArray[0].AsInt32.ShouldEqual((int)source_dynamic.intChildren[0]);
    [Fact] void should_have_second_integer_child_with_correct_value() => result.GetElement("intChildren").Value.AsBsonArray[1].AsInt32.ShouldEqual((int)source_dynamic.intChildren[1]);
    [Fact] void should_have_third_integer_child_with_correct_value() => result.GetElement("intChildren").Value.AsBsonArray[2].AsInt32.ShouldEqual((int)source_dynamic.intChildren[2]);

    [Fact] void should_have_first_string_child_with_correct_value() => result.GetElement("stringChildren").Value.AsBsonArray[0].AsString.ShouldEqual((string)source_dynamic.stringChildren[0]);
    [Fact] void should_have_second_string_child_with_correct_value() => result.GetElement("stringChildren").Value.AsBsonArray[1].AsString.ShouldEqual((string)source_dynamic.stringChildren[1]);
    [Fact] void should_have_third_string_child_with_correct_value() => result.GetElement("stringChildren").Value.AsBsonArray[2].AsString.ShouldEqual((string)source_dynamic.stringChildren[2]);
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_complex_structure_to_expando_object : given.an_expando_object_converter
{
    BsonDocument source;
    BsonDocument reference;
    BsonDocument child;
    dynamic result;

    void Establish()
    {
        reference = new BsonDocument(new BsonElement[]
        {
            new BsonElement("intValue", 43),
            new BsonElement("floatValue", 43.43),
            new BsonElement("doubleValue", 43.43),
            new BsonElement("guidValue", new BsonBinaryData(Guid.Parse("4f8cef8b-0443-4e4b-9c94-42fac316b241"), GuidRepresentation.Standard))
        }.AsEnumerable());

        child = new BsonDocument(new BsonElement[]
        {
            new BsonElement("intValue", 44),
            new BsonElement("floatValue", 44.44),
            new BsonElement("doubleValue", 44.44),
            new BsonElement("guidValue", "e0771dc1-0f2a-482a-9c1f-14afaf155716")
        }.AsEnumerable());

        source = new BsonDocument(new BsonElement[]
        {
            new BsonElement("intValue", 42),
            new BsonElement("floatValue", 42.42),
            new BsonElement("doubleValue", 42.42),
            new BsonElement("enumValue", 1),
            new BsonElement("guidValue", "251b9fbe-83d4-4306-9a5d-9d0e7d4dd456"),
            new BsonElement("dateTimeValue", new BsonDateTime(DateTime.Parse("2022-10-31T14:51:32.8450000Z"))),
            new BsonElement("dateTimeOffsetValue", new BsonDateTime(DateTime.Parse("2022-10-31T14:51:32.8450000Z"))),
            new BsonElement("dateOnlyValue", new BsonDateTime(DateTime.Parse("2022-10-31T14:51:32.8450000Z"))),
            new BsonElement("timeOnlyValue", new BsonDateTime(DateTime.Parse("2022-10-31T14:51:32.8450000Z"))),
            new BsonElement("reference", reference),
            new BsonElement("intChildren", new BsonArray(new BsonValue[]
            {
                new BsonInt32(1),
                new BsonInt32(2),
                new BsonInt32(3)
            }.AsEnumerable())),
            new BsonElement("stringChildren", new BsonArray(new BsonValue[]
            {
                new BsonString("first"),
                new BsonString("second"),
                new BsonString("third")
            }.AsEnumerable())),
            new BsonElement("children", new BsonArray(new BsonDocument[]
            {
                child
            })),
            new BsonElement("stringDictionary", new BsonDocument(new BsonElement[]
            {
                new BsonElement("first", "firstValue"),
                new BsonElement("second", "secondValue")
            }.AsEnumerable())),
            new BsonElement("intDictionary", new BsonDocument(new BsonElement[]
            {
                new BsonElement("first", 42),
                new BsonElement("second", 43)
            }.AsEnumerable())),
            new BsonElement("complexTypeDictionary", new BsonDocument(new BsonElement[]
            {
                new BsonElement("first", new BsonDocument(new BsonElement[]
                {
                    new BsonElement("intValue", 42),
                    new BsonElement("floatValue", 42.42),
                    new BsonElement("doubleValue", 42.42),
                    new BsonElement("guidValue", "251b9fbe-83d4-4306-9a5d-9d0e7d4dd456")
                }.AsEnumerable()))
            }.AsEnumerable())),
        }.AsEnumerable());
    }

    void Because() => result = converter.ToExpandoObject(source, schema);

    [Fact] void should_set_top_level_int_value_to_be_of_int_type() => ((object)result.intValue).ShouldBeOfExactType<int>();
    [Fact] void should_set_top_level_int_value_to_hold_correct_value() => ((int)result.intValue).ShouldEqual(source.GetElement("intValue").Value.AsInt32);
    [Fact] void should_set_top_level_float_value_to_be_of_float_type() => ((object)result.floatValue).ShouldBeOfExactType<float>();
    [Fact] void should_set_top_level_float_value_to_hold_correct_value() => ((float)result.floatValue).ShouldEqual((float)source.GetElement("floatValue").Value.AsDouble);
    [Fact] void should_set_top_level_double_value_to_be_of_float_type() => ((object)result.doubleValue).ShouldBeOfExactType<double>();
    [Fact] void should_set_top_level_double_value_to_hold_correct_value() => ((double)result.doubleValue).ShouldEqual(source.GetElement("doubleValue").Value.AsDouble);
    [Fact] void should_set_top_level_enum_value_to_be_of_float_type() => ((object)result.enumValue).ShouldBeOfExactType<int>();
    [Fact] void should_set_top_level_enum_value_to_hold_correct_value() => ((double)result.enumValue).ShouldEqual(source.GetElement("enumValue").Value.AsInt32);
    [Fact] void should_set_top_level_guid_value_to_be_of_guid_type() => ((object)result.guidValue).ShouldBeOfExactType<Guid>();
    [Fact] void should_set_top_level_guid_value_to_hold_correct_value() => ((Guid)result.guidValue).ShouldEqual(Guid.Parse(source.GetElement("guidValue").Value.AsString));
    [Fact] void should_set_top_level_date_time_value_to_be_of_date_time_type() => ((object)result.dateTimeValue).ShouldBeOfExactType<DateTime>();
    [Fact] void should_set_top_level_date_time_value_to_hold_correct_value() => ((DateTime)result.dateTimeValue).ShouldEqual(source.GetElement("dateTimeValue").Value.ToUniversalTime());
    [Fact] void should_set_top_level_date_time_offset_value_to_be_of_date_time_offset_type() => ((object)result.dateTimeOffsetValue).ShouldBeOfExactType<DateTimeOffset>();
    [Fact] void should_set_top_level_date_time_offset_value_to_hold_correct_value() => ((DateTimeOffset)result.dateTimeOffsetValue).ShouldEqual(DateTimeOffset.FromUnixTimeMilliseconds(((BsonDateTime)source.GetElement("dateTimeOffsetValue").Value).MillisecondsSinceEpoch));
    [Fact] void should_set_top_level_date_only_value_to_be_of_date_time_offset_type() => ((object)result.dateOnlyValue).ShouldBeOfExactType<DateOnly>();
    [Fact] void should_set_top_level_date_only_value_to_hold_correct_value() => ((DateOnly)result.dateOnlyValue).ShouldEqual(DateOnly.FromDateTime(source.GetElement("dateOnlyValue").Value.ToUniversalTime()));
    [Fact] void should_set_top_level_time_only_value_to_be_of_date_time_offset_type() => ((object)result.timeOnlyValue).ShouldBeOfExactType<TimeOnly>();
    [Fact] void should_set_top_level_time_only_value_to_hold_correct_value() => ((TimeOnly)result.timeOnlyValue).ShouldEqual(TimeOnly.FromDateTime(source.GetElement("timeOnlyValue").Value.ToUniversalTime()));
    [Fact] void should_set_top_level_string_dictionary_first_item() => ((string)result.stringDictionary["first"]).ShouldEqual(source.GetElement("stringDictionary").Value.AsBsonDocument.GetElement("first").Value.AsString);
    [Fact] void should_set_top_level_string_dictionary_second_item() => ((string)result.stringDictionary["second"]).ShouldEqual(source.GetElement("stringDictionary").Value.AsBsonDocument.GetElement("second").Value.AsString);
    [Fact] void should_set_top_level_int_dictionary_first_item() => ((int)result.intDictionary["first"]).ShouldEqual(source.GetElement("intDictionary").Value.AsBsonDocument.GetElement("first").Value.AsInt32);
    [Fact] void should_set_top_level_int_dictionary_second_item() => ((int)result.intDictionary["second"]).ShouldEqual(source.GetElement("intDictionary").Value.AsBsonDocument.GetElement("second").Value.AsInt32);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_int_value() => ((int)result.complexTypeDictionary["first"].intValue).ShouldEqual(source.GetElement("complexTypeDictionary").Value.AsBsonDocument.GetElement("first").Value.AsBsonDocument.GetElement("intValue").Value.AsInt32);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_float_value() => ((float)result.complexTypeDictionary["first"].floatValue).ShouldEqual((float)source.GetElement("complexTypeDictionary").Value.AsBsonDocument.GetElement("first").Value.AsBsonDocument.GetElement("floatValue").Value.AsDouble);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_double_value() => ((double)result.complexTypeDictionary["first"].doubleValue).ShouldEqual(source.GetElement("complexTypeDictionary").Value.AsBsonDocument.GetElement("first").Value.AsBsonDocument.GetElement("doubleValue").Value.AsDouble);
    [Fact] void should_set_top_level_complex_type_dictionary_first_item_correct_guid_value() => ((Guid)result.complexTypeDictionary["first"].guidValue).ShouldEqual(Guid.Parse(source.GetElement("complexTypeDictionary").Value.AsBsonDocument.GetElement("first").Value.AsBsonDocument.GetElement("guidValue").Value.AsString));

    [Fact] void should_reference_level_int_value_to_be_of_int_type() => ((object)result.reference.intValue).ShouldBeOfExactType<int>();
    [Fact] void should_reference_level_int_value_to_hold_correct_value() => ((int)result.reference.intValue).ShouldEqual(reference.GetElement("intValue").Value.AsInt32);
    [Fact] void should_reference_level_float_value_to_be_of_float_type() => ((object)result.reference.floatValue).ShouldBeOfExactType<float>();
    [Fact] void should_reference_level_float_value_to_hold_correct_value() => ((float)result.reference.floatValue).ShouldEqual((float)reference.GetElement("floatValue").Value.AsDouble);
    [Fact] void should_reference_level_double_value_to_be_of_float_type() => ((object)result.reference.doubleValue).ShouldBeOfExactType<double>();
    [Fact] void should_reference_level_double_value_to_hold_correct_value() => ((double)result.reference.doubleValue).ShouldEqual(reference.GetElement("doubleValue").Value.AsDouble);
    [Fact] void should_reference_level_guid_value_to_be_of_guid_type() => ((object)result.reference.guidValue).ShouldBeOfExactType<Guid>();
    [Fact] void should_reference_level_guid_value_to_hold_correct_value() => ((Guid)result.reference.guidValue).ShouldEqual(reference.GetElement("guidValue").Value.AsGuid);

    [Fact] void should_child_int_value_to_be_of_int_type() => ((object)result.children[0].intValue).ShouldBeOfExactType<int>();
    [Fact] void should_child_int_value_to_hold_correct_value() => ((int)result.children[0].intValue).ShouldEqual(child.GetElement("intValue").Value.AsInt32);
    [Fact] void should_child_float_value_to_be_of_float_type() => ((object)result.children[0].floatValue).ShouldBeOfExactType<float>();
    [Fact] void should_child_float_value_to_hold_correct_value() => ((float)result.children[0].floatValue).ShouldEqual((float)child.GetElement("floatValue").Value.AsDouble);
    [Fact] void should_child_double_value_to_be_of_float_type() => ((object)result.children[0].doubleValue).ShouldBeOfExactType<double>();
    [Fact] void should_child_double_value_to_hold_correct_value() => ((double)result.children[0].doubleValue).ShouldEqual(child.GetElement("doubleValue").Value.AsDouble);
    [Fact] void should_child_guid_value_to_be_of_guid_type() => ((object)result.children[0].guidValue).ShouldBeOfExactType<Guid>();
    [Fact] void should_child_guid_value_to_hold_correct_value() => ((Guid)result.children[0].guidValue).ShouldEqual(Guid.Parse(child.GetElement("guidValue").Value.AsString));

    [Fact] void should_have_first_integer_child_with_correct_value() => ((int)result.intChildren[0]).ShouldEqual(1);
    [Fact] void should_have_second_integer_child_with_correct_value() => ((int)result.intChildren[1]).ShouldEqual(2);
    [Fact] void should_have_third_integer_child_with_correct_value() => ((int)result.intChildren[2]).ShouldEqual(3);

    [Fact] void should_have_first_string_child_with_correct_value() => ((string)result.stringChildren[0]).ShouldEqual("first");
    [Fact] void should_have_second_string_child_with_correct_value() => ((string)result.stringChildren[1]).ShouldEqual("second");
    [Fact] void should_have_third_string_child_with_correct_value() => ((string)result.stringChildren[2]).ShouldEqual("third");
}

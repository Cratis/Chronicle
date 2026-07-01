// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

/// <summary>
/// Reproduces the reported "bare-concept list persists empty to MongoDB" behavior at the sink's BSON
/// conversion layer (no MongoDB server needed — the conversion is a pure in-process operation).
/// </summary>
public class when_converting_expando_object_with_a_concept_list_to_bson_document : Specification
{
    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    ExpandoObject _source;
    Guid _id;
    Guid _firstRef;
    BsonDocument _result;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromType<ConceptListTargetType>();
        _id = Guid.NewGuid();
        _firstRef = Guid.NewGuid();

        dynamic expando = new ExpandoObject();
        expando.id = _id;
        expando.tags = new List<object> { new BareStringConcept("red"), new BareStringConcept("green") };
        expando.refs = new List<object> { new GuidConcept(_firstRef), new GuidConcept(Guid.NewGuid()) };
        _source = expando;
    }

    ExpandoObject _roundTripped;

    void Because()
    {
        _result = _converter.ToBsonDocument(_source, _schema);
        _roundTripped = _converter.ToExpandoObject(_result, _schema);
    }

    [Fact] void should_have_a_tags_array() => _result.GetElement("tags").Value.IsBsonArray.ShouldBeTrue();
    [Fact] void should_have_two_tags() => _result.GetElement("tags").Value.AsBsonArray.Count.ShouldEqual(2);
    [Fact] void should_persist_first_tag_as_its_underlying_string() => _result.GetElement("tags").Value.AsBsonArray[0].AsString.ShouldEqual("red");
    [Fact] void should_persist_second_tag_as_its_underlying_string() => _result.GetElement("tags").Value.AsBsonArray[1].AsString.ShouldEqual("green");
    [Fact] void should_round_trip_the_tags() => ((IEnumerable<object>)((IDictionary<string, object?>)_roundTripped)["tags"]!).Count().ShouldEqual(2);
    [Fact] void should_round_trip_first_tag_value() => ((IEnumerable<object>)((IDictionary<string, object?>)_roundTripped)["tags"]!).First().ToString().ShouldEqual("red");
    [Fact] void should_have_two_refs() => _result.GetElement("refs").Value.AsBsonArray.Count.ShouldEqual(2);
    [Fact] void should_persist_first_ref_as_its_underlying_guid() => _result.GetElement("refs").Value.AsBsonArray[0].AsGuid.ShouldEqual(_firstRef);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter;

/// <summary>
/// Reproduces the changeset conversion path (ChangesetConverter -> MongoDBConverter.ToBsonValue) for a
/// property that is a list of bare concepts, ensuring each element persists as its underlying value.
/// </summary>
public class when_converting_a_concept_list_to_bson_value : Specification
{
    MongoDBConverter _converter;
    BsonValue _result;

    void Establish()
    {
        var typeFormats = new TypeFormats();
        var model = new ReadModelDefinition(
            typeof(TaggedReadModel).FullName,
            nameof(TaggedReadModel),
            nameof(TaggedReadModel),
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, JsonSchema.FromType<TaggedReadModel>() },
            },
            []);
        _converter = new(new ExpandoObjectConverter(typeFormats), typeFormats, model);
    }

    void Because() => _result = _converter.ToBsonValue(
        new object[] { new TagConcept("red"), new TagConcept("green") },
        new PropertyPath("tags"));

    [Fact] void should_be_an_array() => _result.IsBsonArray.ShouldBeTrue();
    [Fact] void should_have_two_elements() => _result.AsBsonArray.Count.ShouldEqual(2);
    [Fact] void should_store_first_as_underlying_string() => _result.AsBsonArray[0].AsString.ShouldEqual("red");
    [Fact] void should_store_second_as_underlying_string() => _result.AsBsonArray[1].AsString.ShouldEqual("green");
}

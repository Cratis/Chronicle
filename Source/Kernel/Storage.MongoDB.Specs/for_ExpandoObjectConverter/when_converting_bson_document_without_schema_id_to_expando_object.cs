// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_bson_document_without_schema_id_to_expando_object : given.an_expando_object_converter
{
    BsonDocument _source;
    string _eventSourceIdValue;
    dynamic _result;
    IDictionary<string, object> _resultAsDictionary;

    void Establish()
    {
        _eventSourceIdValue = "aaaabbbb-cccc-dddd-eeee-ffffffffffff";
        _source = new BsonDocument
        {
            { "_id", new BsonString(_eventSourceIdValue) },
            { "intValue", 42 }
        };
    }

    void Because()
    {
        _result = converter.ToExpandoObject(_source, schema);
        _resultAsDictionary = (IDictionary<string, object>)_result;
    }

    [Fact] void should_keep_underscore_id_property() => _resultAsDictionary.ContainsKey("_id").ShouldBeTrue();
    [Fact] void should_not_have_camel_case_id_property() => _resultAsDictionary.ContainsKey("id").ShouldBeFalse();
    [Fact] void should_set_underscore_id_to_event_source_id_value() => ((Guid)_result._id).ShouldEqual(Guid.Parse(_eventSourceIdValue));
}

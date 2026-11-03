// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage;
using NJsonSchema;

namespace Cratis.Chronicle.ReadModels.for_ReadModelInstanceExtensions.when_removing_sink_bookkeeping;

public class and_the_schema_does_not_declare_them : Specification
{
    ExpandoObject _instance;
    JsonSchema _schema;
    IDictionary<string, object?> _result;

    void Establish()
    {
        _schema = new JsonSchema();
        _schema.Properties["name"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        _instance = new ExpandoObject();
        var values = (IDictionary<string, object?>)_instance;
        values["name"] = "the name";
        values[WellKnownProperties.AppliedEventMarkers] = new ExpandoObject();
        values[WellKnownProperties.ReadModelInstanceInitialized] = true;
        values[WellKnownProperties.Subject] = "subject";
        values[WellKnownProperties.Subjects] = new ExpandoObject();
        values[WellKnownProperties.LastHandledEventSequenceNumber] = 42UL;
    }

    void Because() => _result = _instance.WithoutSinkBookkeeping(_schema);

    [Fact] void should_keep_the_declared_property() => _result["name"].ShouldEqual("the name");
    [Fact] void should_strip_the_applied_event_markers() => _result.ContainsKey(WellKnownProperties.AppliedEventMarkers).ShouldBeFalse();
    [Fact] void should_strip_the_initialization_state() => _result.ContainsKey(WellKnownProperties.ReadModelInstanceInitialized).ShouldBeFalse();
    [Fact] void should_strip_the_subject() => _result.ContainsKey(WellKnownProperties.Subject).ShouldBeFalse();
    [Fact] void should_strip_the_subjects() => _result.ContainsKey(WellKnownProperties.Subjects).ShouldBeFalse();
    [Fact] void should_keep_the_last_handled_event_sequence_number() => _result[WellKnownProperties.LastHandledEventSequenceNumber].ShouldEqual(42UL);
    [Fact] void should_not_change_the_stored_instance() => ((IDictionary<string, object?>)_instance).ContainsKey(WellKnownProperties.AppliedEventMarkers).ShouldBeTrue();
}

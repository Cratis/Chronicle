// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels.for_ReadModelInstanceExtensions.when_removing_sink_bookkeeping;

public class and_the_schema_declares_one_of_them : Specification
{
    ExpandoObject _instance;
    JsonSchema _schema;
    IDictionary<string, object?> _result;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""{ "type": "object", "properties": { "__applied": { "type": "object" } } }""");
        _instance = new ExpandoObject();
        var values = (IDictionary<string, object?>)_instance;
        values[WellKnownProperties.AppliedEventMarkers] = new ExpandoObject();
        values[WellKnownProperties.ReadModelInstanceInitialized] = true;
    }

    void Because() => _result = _instance.WithoutSinkBookkeeping(_schema);

    [Fact] void should_keep_the_declared_one() => _result.ContainsKey(WellKnownProperties.AppliedEventMarkers).ShouldBeTrue();
    [Fact] void should_strip_the_undeclared_one() => _result.ContainsKey(WellKnownProperties.ReadModelInstanceInitialized).ShouldBeFalse();
}

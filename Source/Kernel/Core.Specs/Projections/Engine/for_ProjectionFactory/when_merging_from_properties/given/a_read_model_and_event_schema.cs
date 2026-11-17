// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_merging_from_properties.given;

public class a_read_model_and_event_schema : Specification
{
    protected JsonSchema _readModelSchema;
    protected JsonSchema _eventSchema;
    protected IReadOnlySet<string> _noAutoMap = new HashSet<string>();

    async Task Establish()
    {
        _readModelSchema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"name":{"type":"string"},"total":{"type":"integer"},"other":{"type":"string"}}}
            """);
        _eventSchema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"name":{"type":"string"},"total":{"type":"integer"},"unrelated":{"type":"string"}}}
            """);
    }

    protected static FromDefinition FromWith(params (string Property, string Expression)[] mappings) =>
        new(mappings.ToDictionary(_ => (PropertyPath)_.Property, _ => _.Expression), PropertyExpression.NotSet, null);
}

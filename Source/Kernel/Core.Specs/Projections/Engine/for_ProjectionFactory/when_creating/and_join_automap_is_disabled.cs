// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_join_automap_is_disabled : Specification
{
    IReadOnlyList<KeyValuePair<PropertyPath, string>> _disabled;
    IReadOnlyList<KeyValuePair<PropertyPath, string>> _inherited;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"name":{"type":"string"}}}
            """);
        var properties = new Dictionary<PropertyPath, string>();
        _disabled = ProjectionFactory.GetMergedJoinProperties(
            new JoinDefinition((PropertyPath)"id", properties, PropertyExpression.NotSet, AutoMap.Disabled),
            schema,
            schema,
            AutoMap.Enabled,
            new HashSet<string>());
        _inherited = ProjectionFactory.GetMergedJoinProperties(
            new JoinDefinition((PropertyPath)"id", properties, PropertyExpression.NotSet),
            schema,
            schema,
            AutoMap.Enabled,
            new HashSet<string>());
    }

    [Fact] void should_not_map_same_named_properties_when_disabled() => _disabled.ShouldBeEmpty();
    [Fact] void should_map_same_named_properties_when_inherited() => _inherited.ShouldContain(_ => _.Key == (PropertyPath)"name");
}

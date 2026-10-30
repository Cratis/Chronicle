// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_no_key_is_the_bare_word : given.a_projection_definition
{
    void Establish() => _definition = Definition(
        from: new Dictionary<EventType, FromDefinition> { [_type] = From("id"), [_otherType] = From(" EventSourceId") },
        children: new Dictionary<PropertyPath, ChildrenDefinition> { ["children"] = Child(from: new Dictionary<EventType, FromDefinition> { [_type] = From("$eventSourceId") }) });

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_return_the_original_definition() => _result.ShouldBeSame(_definition);
    [Fact] void should_return_the_original_child() => _result.Children["children"].ShouldBeSame(_definition.Children["children"]);
    [Fact] void should_not_report_a_rewrite() => _rewritten.ShouldBeEmpty();
}

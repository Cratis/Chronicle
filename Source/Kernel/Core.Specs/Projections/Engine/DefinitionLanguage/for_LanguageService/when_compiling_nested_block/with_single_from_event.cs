// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_nested_block;

public class with_single_from_event : given.a_language_service_compiling_nested<given.SliceReadModel>
{
    const string Declaration = """
        projection Slice => SliceReadModel
          from SliceCreated
            name = name

          nested command
            from CommandSetForSlice
              name = name
              schema = schema
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.SliceCreated), typeof(given.CommandSetForSlice)];

    ProjectionDefinition _result;
    ChildrenDefinition _nested;

    void Because()
    {
        _result = Compile(Declaration);
        _nested = _result.Nested[(PropertyPath)"command"];
    }

    [Fact] void should_have_a_nested_entry() => _result.Nested.ShouldNotBeNull();
    [Fact] void should_have_a_nested_entry_keyed_by_property_name() => _result.Nested.ContainsKey((PropertyPath)"command").ShouldBeTrue();
    [Fact] void should_have_identified_by_not_set() => _nested.IdentifiedBy.IsSet.ShouldBeFalse();
    [Fact] void should_have_the_from_event() => _nested.From.ContainsKey((EventType)"CommandSetForSlice").ShouldBeTrue();
    [Fact] void should_map_name_property() => _nested.From[(EventType)"CommandSetForSlice"].Properties[(PropertyPath)"name"].ShouldEqual("name");
    [Fact] void should_map_schema_property() => _nested.From[(EventType)"CommandSetForSlice"].Properties[(PropertyPath)"schema"].ShouldEqual("schema");
}

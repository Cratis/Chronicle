// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_nested_block.and_generating_it_back;

public class and_it_is_at_the_root_with_an_inner_nested : given.a_language_service_compiling_nested<given.SliceWithInnerNestedReadModel>
{
    const string Declaration = """
        projection Slice => SliceWithInnerNestedReadModel
          from SliceCreated
            name = name

          nested command
            from CommandSetForSlice
              name = name
              schema = schema

            nested validation
              from ValidationAdded
                rule = rule
              clear with ValidationRemoved

            clear with CommandClearedForSlice
        """;

    protected override IEnumerable<Type> EventTypes =>
        [typeof(given.SliceCreated), typeof(given.CommandSetForSlice), typeof(given.CommandClearedForSlice), typeof(given.ValidationAdded), typeof(given.ValidationRemoved)];

    ProjectionDefinition _result;
    ChildrenDefinition _outer;
    ChildrenDefinition _inner;

    void Because()
    {
        _result = CompileGenerateAndRecompile(Declaration);
        _outer = _result.Nested[(PropertyPath)"command"];
        _inner = _outer.Nested[(PropertyPath)"validation"];
    }

    [Fact] void should_keep_the_root_nested() => _result.Nested.ContainsKey((PropertyPath)"command").ShouldBeTrue();
    [Fact] void should_keep_its_from_event() => _outer.From.ContainsKey((EventType)"CommandSetForSlice").ShouldBeTrue();
    [Fact] void should_keep_its_mappings() => _outer.From[(EventType)"CommandSetForSlice"].Properties[new PropertyPath("schema")].ShouldEqual("schema");
    [Fact] void should_keep_its_clear_with() => _outer.RemovedWith.ContainsKey((EventType)"CommandClearedForSlice").ShouldBeTrue();
    [Fact] void should_keep_the_inner_nested() => _outer.Nested.ContainsKey((PropertyPath)"validation").ShouldBeTrue();
    [Fact] void should_keep_the_inner_from_event() => _inner.From.ContainsKey((EventType)"ValidationAdded").ShouldBeTrue();
    [Fact] void should_keep_the_inner_clear_with() => _inner.RemovedWith.ContainsKey((EventType)"ValidationRemoved").ShouldBeTrue();
}

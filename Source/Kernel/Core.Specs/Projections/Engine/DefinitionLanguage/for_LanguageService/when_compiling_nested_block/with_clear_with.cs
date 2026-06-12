// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_nested_block;

public class with_clear_with : given.a_language_service_compiling_nested<given.SliceReadModel>
{
    const string Declaration = """
        projection Slice => SliceReadModel
          from SliceCreated
            name = name

          nested command
            from CommandSetForSlice
              name = name
              schema = schema
            clear with CommandClearedForSlice
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.SliceCreated), typeof(given.CommandSetForSlice), typeof(given.CommandClearedForSlice)];

    ProjectionDefinition _result;
    ChildrenDefinition _nested;

    void Because()
    {
        _result = Compile(Declaration);
        _nested = _result.Nested[(PropertyPath)"command"];
    }

    [Fact] void should_have_removed_with_for_clear_event() => _nested.RemovedWith.ContainsKey((EventType)"CommandClearedForSlice").ShouldBeTrue();
    [Fact] void should_still_have_the_from_event() => _nested.From.ContainsKey((EventType)"CommandSetForSlice").ShouldBeTrue();
}

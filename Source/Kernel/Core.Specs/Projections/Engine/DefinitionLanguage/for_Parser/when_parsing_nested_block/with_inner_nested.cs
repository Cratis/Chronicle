// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.AST;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_Parser.when_parsing_nested_block;

public class with_inner_nested : given.a_parser
{
    const string Declaration = """
        projection Slice => SliceReadModel
          from SliceCreated
            Name = name

          nested command
            from CommandSetForSlice
              Name = commandName

            nested validation
              from ValidationAdded
                Rule = rule
              clear with ValidationRemoved

            clear with CommandClearedForSlice
        """;

    NestedBlock _outer;
    NestedChildBlock _inner;

    void Because()
    {
        Parse(Declaration);
        _outer = _document.Projections[0].Directives.OfType<NestedBlock>().Single();
        _inner = _outer.ChildBlocks.OfType<NestedChildBlock>().Single();
    }

    [Fact] void should_not_have_errors() => _errors.HasErrors.ShouldBeFalse();
    [Fact] void should_have_an_inner_nested_block() => _inner.ShouldNotBeNull();
    [Fact] void should_have_the_inner_property_name() => _inner.PropertyName.ShouldEqual("validation");
    [Fact] void should_have_inner_from_event() => _inner.ChildBlocks.OfType<ChildOnEventBlock>().Single().EventType.Name.ShouldEqual("ValidationAdded");
    [Fact] void should_have_inner_clear_with() => _inner.ChildBlocks.OfType<ClearWithDirective>().Single().EventType.Name.ShouldEqual("ValidationRemoved");
}

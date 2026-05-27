// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.AST;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_Parser.when_parsing_nested_block;

public class with_single_from_event : given.a_parser
{
    const string Declaration = """
        projection Slice => SliceReadModel
          from SliceCreated
            Name = name

          nested command
            from CommandSetForSlice
              Name = commandName
        """;

    NestedBlock _nestedBlock;

    void Because()
    {
        Parse(Declaration);
        _nestedBlock = _document.Projections[0].Directives.OfType<NestedBlock>().Single();
    }

    [Fact] void should_not_have_errors() => _errors.HasErrors.ShouldBeFalse();
    [Fact] void should_have_a_nested_block() => _nestedBlock.ShouldNotBeNull();
    [Fact] void should_have_the_property_name() => _nestedBlock.PropertyName.ShouldEqual("command");
    [Fact] void should_have_a_single_child_block() => _nestedBlock.ChildBlocks.Count.ShouldEqual(1);
    [Fact] void should_have_the_from_event_block_as_child() => _nestedBlock.ChildBlocks[0].ShouldBeOfExactType<ChildOnEventBlock>();
    [Fact] void should_have_the_from_event_type() => ((ChildOnEventBlock)_nestedBlock.ChildBlocks[0]).EventType.Name.ShouldEqual("CommandSetForSlice");
}

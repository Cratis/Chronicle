// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.AST;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_Parser.when_parsing_nested_block;

public class with_clear_with : given.a_parser
{
    const string Declaration = """
        projection Slice => SliceReadModel
          from SliceCreated
            Name = name

          nested command
            from CommandSetForSlice
              Name = commandName
            clear with CommandClearedForSlice
        """;

    NestedBlock _nestedBlock;
    ClearWithDirective _clearWith;

    void Because()
    {
        Parse(Declaration);
        _nestedBlock = _document.Projections[0].Directives.OfType<NestedBlock>().Single();
        _clearWith = _nestedBlock.ChildBlocks.OfType<ClearWithDirective>().Single();
    }

    [Fact] void should_not_have_errors() => _errors.HasErrors.ShouldBeFalse();
    [Fact] void should_have_a_clear_with_directive() => _clearWith.ShouldNotBeNull();
    [Fact] void should_have_the_clear_event_type() => _clearWith.EventType.Name.ShouldEqual("CommandClearedForSlice");
}

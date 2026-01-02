// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_remove_with_block : Specification
{
    const string definition = """
        projection Group => GroupReadModel
          children Members id e.userId
            from UserAddedToGroup
              key e.userId
              parent e.groupId
            remove on UserRemovedFromGroup
              key e.userId
              parent e.groupId
        """;

    ChildrenBlock _childrenBlock;
    RemoveBlock _removeBlock;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        var result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _childrenBlock = (ChildrenBlock)result.Projections[0].Directives[0];
        _removeBlock = (RemoveBlock)_childrenBlock.ChildBlocks[1];
    }

    [Fact] void should_have_remove_block() => _removeBlock.ShouldNotBeNull();
    [Fact] void should_have_event_type() => _removeBlock.EventType.Name.ShouldEqual("UserRemovedFromGroup");
    [Fact] void should_have_key() => _removeBlock.Key.ShouldNotBeNull();
    [Fact] void should_have_parent_key() => _removeBlock.ParentKey.ShouldNotBeNull();
}

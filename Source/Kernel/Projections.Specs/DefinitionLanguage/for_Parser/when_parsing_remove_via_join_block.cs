// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_remove_via_join_block : Specification
{
    const string definition = """
        projection User => UserReadModel
          children Groups id e.groupId
            from UserAddedToGroup
              parent e.userId
            join Group on GroupId
              events GroupCreated, GroupRenamed
              automap
            remove via join on GroupDeleted
        """;

    ChildrenBlock _childrenBlock;
    RemoveViaJoinBlock _removeViaJoinBlock;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        var result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _childrenBlock = (ChildrenBlock)result.Projections[0].Directives[0];
        _removeViaJoinBlock = (RemoveViaJoinBlock)_childrenBlock.ChildBlocks[2];
    }

    [Fact] void should_have_remove_via_join_block() => _removeViaJoinBlock.ShouldNotBeNull();
    [Fact] void should_have_event_type() => _removeViaJoinBlock.EventType.Name.ShouldEqual("GroupDeleted");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_children_with_join : Specification
{
    const string definition = """
        projection User => UserReadModel
          children Groups id e.groupId
            from UserAddedToGroup
              key e.groupId
              parent e.userId
            join Group on GroupId
              events GroupCreated, GroupRenamed
              automap
        """;

    ChildrenBlock _childrenBlock;
    ChildJoinBlock _joinBlock;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        var result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _childrenBlock = (ChildrenBlock)result.Projections[0].Directives[0];
        _joinBlock = (ChildJoinBlock)_childrenBlock.ChildBlocks[1];
    }

    [Fact] void should_have_join_in_children() => _joinBlock.ShouldNotBeNull();
    [Fact] void should_have_join_name() => _joinBlock.JoinName.ShouldEqual("Group");
    [Fact] void should_have_join_property() => _joinBlock.OnProperty.ShouldEqual("GroupId");
    [Fact] void should_have_event_types() => _joinBlock.EventTypes.Count.ShouldEqual(2);
    [Fact] void should_have_automap_enabled() => _joinBlock.AutoMap.ShouldBeTrue();
}

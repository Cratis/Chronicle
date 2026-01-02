// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_children_block : Specification
{
    const string definition = """
        projection Group => GroupReadModel
          children Members id e.userId
            automap
            from UserAddedToGroup
              key e.userId
              parent ctx.eventSourceId
              Role = e.role
            from UserRoleChanged
              key e.userId
              parent e.groupId
              Role = e.role
        """;

    ChildrenBlock _childrenBlock;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        var result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _childrenBlock = (ChildrenBlock)result.Projections[0].Directives[0];
    }

    [Fact] void should_have_collection_name() => _childrenBlock.CollectionName.ShouldEqual("Members");
    [Fact] void should_have_identifier_expression() => _childrenBlock.IdentifierExpression.ShouldNotBeNull();
    [Fact] void should_have_automap_enabled() => _childrenBlock.AutoMap.ShouldBeTrue();
    [Fact] void should_have_two_child_blocks() => _childrenBlock.ChildBlocks.Count.ShouldEqual(2);
    [Fact] void should_have_child_on_event_blocks() => _childrenBlock.ChildBlocks.OfType<ChildOnEventBlock>().Count().ShouldEqual(2);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_join_block : Specification
{
    const string definition = """
        projection UserGroups => UserGroupReadModel
          join Group on GroupId
            events GroupCreated, GroupRenamed
            automap
        """;

    JoinBlock _joinBlock;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        var result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _joinBlock = (JoinBlock)result.Projections[0].Directives[0];
    }

    [Fact] void should_have_join_name() => _joinBlock.JoinName.ShouldEqual("Group");
    [Fact] void should_have_join_key() => _joinBlock.OnProperty.ShouldEqual("GroupId");
    [Fact] void should_have_two_event_types() => _joinBlock.EventTypes.Count.ShouldEqual(2);
    [Fact] void should_have_group_created_event() => _joinBlock.EventTypes[0].Name.ShouldEqual("GroupCreated");
    [Fact] void should_have_group_renamed_event() => _joinBlock.EventTypes[1].Name.ShouldEqual("GroupRenamed");
    [Fact] void should_have_automap_enabled() => _joinBlock.AutoMap.ShouldBeTrue();
}

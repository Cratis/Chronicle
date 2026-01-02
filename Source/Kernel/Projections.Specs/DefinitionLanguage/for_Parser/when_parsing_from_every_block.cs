// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_from_every_block : Specification
{
    const string definition = """
        projection Test => Model
          every
            LastUpdated = ctx.occurred
            EventSourceId = ctx.eventSourceId
            exclude children
        """;

    EveryBlock _everyBlock;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        var result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _everyBlock = (EveryBlock)result.Projections[0].Directives[0];
    }

    [Fact] void should_have_two_mappings() => _everyBlock.Mappings.Count.ShouldEqual(2);
    [Fact] void should_exclude_children() => _everyBlock.ExcludeChildren.ShouldBeTrue();
    [Fact] void should_have_assignment_operations() => _everyBlock.Mappings.All(m => m is AssignmentOperation).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class children_and_join : given.a_language_service
{
    const string definition = """
        projection UserGroup => UserGroupReadModel
          children Members id userId
            from UserAdded key userId

          join GroupSettings on SettingsId
            events SettingsCreated
        """;

    Document _result;
    ProjectionNode _projection;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        _result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors.Select(e => $"Line {e.Line}, Col {e.Column}: {e.Message}"))}"));
        _projection = _result.Projections[0];
    }

    [Fact] void should_have_projection() => _projection.ShouldNotBeNull();
    [Fact] void should_have_children_block() => _projection.Directives.OfType<ChildrenBlock>().ShouldNotBeEmpty();
    [Fact] void should_have_join_block() => _projection.Directives.OfType<JoinBlock>().ShouldNotBeEmpty();
}

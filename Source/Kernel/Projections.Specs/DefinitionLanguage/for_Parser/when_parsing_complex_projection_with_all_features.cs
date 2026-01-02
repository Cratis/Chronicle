// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_complex_projection_with_all_features : Specification
{
    const string definition = """
        projection UserGroup => UserGroupReadModel
          automap

          every
            LastUpdated = ctx.occurred
            exclude children

          from GroupCreated
            key ctx.eventSourceId
            Name = e.name
            Description = e.description
            CreatedAt = ctx.occurred
            MemberCount = 0

          from GroupRenamed
            key ctx.eventSourceId
            Name = e.newName

          from MemberJoined
            key ctx.eventSourceId
            increment MemberCount

          from MemberLeft
            key ctx.eventSourceId
            decrement MemberCount

          children Members id e.userId
            automap
            from UserAddedToGroup
              key e.userId
              parent ctx.eventSourceId
              Role = e.role
              JoinedAt = ctx.occurred
            from UserRoleChanged
              key e.userId
              parent e.groupId
              Role = e.role
            remove on UserRemovedFromGroup
              key e.userId
              parent e.groupId

          join GroupSettings on SettingsId
            events SettingsCreated, SettingsUpdated
            automap
        """;

    Document _result;
    ProjectionNode _projection;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        _result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _projection = _result.Projections[0];
    }

    [Fact] void should_have_projection() => _projection.ShouldNotBeNull();
    [Fact] void should_have_projection_name() => _projection.Name.ShouldEqual("UserGroup");
    [Fact] void should_have_read_model_type() => _projection.ReadModelType.Name.ShouldEqual("UserGroupReadModel");
    [Fact] void should_have_multiple_directives() => _projection.Directives.Count.ShouldBeGreaterThan(5);
    [Fact] void should_have_automap_directive() => _projection.Directives.OfType<AutoMapDirective>().ShouldNotBeEmpty();
    [Fact] void should_have_every_block() => _projection.Directives.OfType<EveryBlock>().ShouldNotBeEmpty();
    [Fact] void should_have_from_event_blocks() => _projection.Directives.OfType<FromEventBlock>().Count().ShouldBeGreaterThan(3);
    [Fact] void should_have_children_block() => _projection.Directives.OfType<ChildrenBlock>().ShouldNotBeEmpty();
    [Fact] void should_have_join_block() => _projection.Directives.OfType<JoinBlock>().ShouldNotBeEmpty();
    [Fact] void should_have_increment_operation() => _projection.Directives.OfType<FromEventBlock>().Any(b => b.Mappings.Any(m => m is IncrementOperation)).ShouldBeTrue();
    [Fact] void should_have_decrement_operation() => _projection.Directives.OfType<FromEventBlock>().Any(b => b.Mappings.Any(m => m is DecrementOperation)).ShouldBeTrue();
}

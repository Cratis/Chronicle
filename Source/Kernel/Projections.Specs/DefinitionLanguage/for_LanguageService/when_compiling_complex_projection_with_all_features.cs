// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService;

public class when_compiling_complex_projection_with_all_features : given.a_language_service
{
    const string definition = """
        projection UserGroup => UserGroupReadModel
          automap

          every
            LastUpdated = $eventContext.occurred
            exclude children

          from GroupCreated
            key $eventContext.eventSourceId
            Name = name
            Description = description
            CreatedAt = $eventContext.occurred
            MemberCount = 0

          from GroupRenamed
            key $eventContext.eventSourceId
            Name = newName

          from MemberJoined
            key $eventContext.eventSourceId
            increment MemberCount

          from MemberLeft
            key $eventContext.eventSourceId
            decrement MemberCount

          children Members id userId
            automap
            from UserAddedToGroup
              key userId
              parent $eventContext.eventSourceId
              Role = role
              JoinedAt = $eventContext.occurred
            from UserRoleChanged
              key userId
              parent groupId
              Role = role
            remove on UserRemovedFromGroup
              key userId
              parent groupId

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

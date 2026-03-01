// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling;

public class children_with_every_block : for_LanguageService.given.a_language_service_with_schemas<for_LanguageService.given.GroupReadModel>
{
    const string Declaration = """
        projection Group => GroupReadModel
          from GroupCreated
            name = name

          children members identified by userId
            from UserAddedToGroup key userId
              parent groupId
              role = role

            every
              name = userName
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(for_LanguageService.given.GroupCreated), typeof(for_LanguageService.given.UserAddedToGroup)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_children() => _result.Children.ShouldNotBeEmpty();
    [Fact] void should_have_members_children() => _result.Children.ContainsKey((PropertyPath)"members").ShouldBeTrue();
    [Fact] void should_have_every_definition() => _result.Children[(PropertyPath)"members"].All.ShouldNotBeNull();
    [Fact] void should_have_property_in_every() => _result.Children[(PropertyPath)"members"].All.Properties.ContainsKey((PropertyPath)"name").ShouldBeTrue();
    [Fact] void should_map_to_user_name() => _result.Children[(PropertyPath)"members"].All.Properties[(PropertyPath)"name"].ShouldEqual("userName");
}

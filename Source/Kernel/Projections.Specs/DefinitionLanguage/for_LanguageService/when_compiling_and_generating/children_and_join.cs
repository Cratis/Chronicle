// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class children_and_join : given.a_language_service
{
    const string Definition = """
        projection UserGroup => UserGroupReadModel
          children Members id userId
            from UserAdded key userId

          join GroupSettings on SettingsId
            events SettingsCreated
        """;

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition, "UserGroupReadModel");

    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);
    [Fact] void should_have_members_child() => _result.Children.ContainsKey("Members").ShouldBeTrue();
    [Fact] void should_have_join_definition() => _result.Join.Count.ShouldEqual(1);
    [Fact] void should_have_group_settings_join() => _result.Join.ContainsKey("GroupSettings").ShouldBeTrue();
}

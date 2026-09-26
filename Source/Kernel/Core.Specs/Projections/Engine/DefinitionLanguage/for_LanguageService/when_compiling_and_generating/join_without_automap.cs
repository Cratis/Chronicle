// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_and_generating;

public class join_without_automap : given.a_language_service_with_schemas<given.UserGroupReadModel>
{
    const string Declaration = """
        projection UserGroups => UserGroupReadModel
          join Group on groupId
            with GroupCreated
              no automap
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.GroupCreated)];

    given.CompilerResult _result;
    ProjectionDefinition _inherited;

    void Because()
    {
        _result = CompileGenerateAndRecompile(Declaration);
        _inherited = _languageService.Compile(
            Declaration.Replace("no automap", string.Empty, StringComparison.Ordinal),
            ProjectionOwner.Client,
            [_readModelDefinition],
            _eventTypeSchemas)
            .Match(_ => _, _ => throw new InvalidOperationException("Inherit declaration failed to compile"));
    }

    [Fact] void should_preserve_join_automap_in_the_generated_declaration() => _result.GeneratedDefinition.ShouldContain("with GroupCreated\n            no automap");
    [Fact] void should_disable_auto_map_for_the_join() => _result.Definition.Join[(EventType)"GroupCreated"].AutoMap.ShouldEqual(AutoMap.Disabled);
    [Fact] void should_inherit_automap_when_not_specified() => _inherited.Join[(EventType)"GroupCreated"].AutoMap.ShouldEqual(AutoMap.Inherit);
}

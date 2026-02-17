// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class causedby_expressions : given.a_language_service_with_schemas<given.ActivityReadModel>
{
    const string Declaration = """
        projection Activity => ActivityReadModel
          from ActivityLogged
            key $eventSourceId
            createdBySubject = $causedBy.subject
            createdByName = $causedBy.name
            createdByUser = $causedBy.userName
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.ActivityLogged)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_from_definition() => _result.From.ContainsKey((EventType)"ActivityLogged").ShouldBeTrue();
    [Fact] void should_map_created_by_subject() => _result.From[(EventType)"ActivityLogged"].Properties[new PropertyPath("createdBySubject")].ShouldEqual($"{WellKnownExpressions.CausedBy}(subject)");
    [Fact] void should_map_created_by_name() => _result.From[(EventType)"ActivityLogged"].Properties[new PropertyPath("createdByName")].ShouldEqual($"{WellKnownExpressions.CausedBy}(name)");
    [Fact] void should_map_created_by_user() => _result.From[(EventType)"ActivityLogged"].Properties[new PropertyPath("createdByUser")].ShouldEqual($"{WellKnownExpressions.CausedBy}(userName)");
}

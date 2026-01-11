// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class from_all_with_dictionary : given.a_language_service_with_schemas<given.Model>
{
    const string Definition = """
        projection Test => Model
          from $all
            count eventCountByType.$eventContext.type.name
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.ActivityLogged)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_from_every_populated() => _result.FromEvery.ShouldNotBeNull();
    [Fact] void should_have_one_property() => _result.FromEvery.Properties.Count.ShouldEqual(1);
    [Fact] void should_map_event_count_with_dynamic_key() => _result.FromEvery.Properties[new PropertyPath("eventCountByType.$eventContext.type.name")].ShouldEqual(WellKnownExpressions.Count);
    [Fact] void should_include_children() => _result.FromEvery.IncludeChildren.ShouldBeTrue();
}

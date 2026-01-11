// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class from_all_block : given.a_language_service_with_schemas<given.Model>
{
    const string Definition = """
        projection Test => Model
          from $all
            count eventCount
            lastOccurred = $eventContext.occurred
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.ActivityLogged)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_from_every_populated() => _result.FromEvery.ShouldNotBeNull();
    [Fact] void should_have_two_properties() => _result.FromEvery.Properties.Count.ShouldEqual(2);
    [Fact] void should_map_event_count() => _result.FromEvery.Properties[new PropertyPath("eventCount")].ShouldEqual(WellKnownExpressions.Count);
    [Fact] void should_map_last_occurred() => _result.FromEvery.Properties[new PropertyPath("lastOccurred")].ShouldEqual($"{WellKnownExpressions.EventContext}(occurred)");
    [Fact] void should_include_children() => _result.FromEvery.IncludeChildren.ShouldBeTrue();
    [Fact] void should_have_automap_enabled() => _result.FromEvery.AutoMap.ShouldEqual(AutoMap.Enabled);
}

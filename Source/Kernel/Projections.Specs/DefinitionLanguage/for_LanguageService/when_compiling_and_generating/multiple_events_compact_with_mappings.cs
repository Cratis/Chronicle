// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class multiple_events_compact_with_mappings : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Definition = """
        projection User => UserReadModel
          from EventA key keyA, EventB key keyB, EventC
            automap
            sharedProperty = sharedValue
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.EventA), typeof(given.EventB), typeof(given.EventC)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_from_event_a() => _result.From.ContainsKey((EventType)"EventA").ShouldBeTrue();
    [Fact] void should_have_from_event_b() => _result.From.ContainsKey((EventType)"EventB").ShouldBeTrue();
    [Fact] void should_have_from_event_c() => _result.From.ContainsKey((EventType)"EventC").ShouldBeTrue();
    [Fact] void should_have_event_a_key() => _result.From[(EventType)"EventA"].Key.Value.ShouldEqual("keyA");
    [Fact] void should_have_event_b_key() => _result.From[(EventType)"EventB"].Key.Value.ShouldEqual("keyB");
    [Fact] void should_not_have_event_c_key() => _result.From[(EventType)"EventC"].Key.IsSet().ShouldBeFalse();
    [Fact] void should_have_automap_on_event_a() => _result.From[(EventType)"EventA"].AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_have_automap_on_event_b() => _result.From[(EventType)"EventB"].AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_have_automap_on_event_c() => _result.From[(EventType)"EventC"].AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_have_shared_property_on_event_a() => _result.From[(EventType)"EventA"].Properties.ContainsKey(new PropertyPath("sharedProperty")).ShouldBeTrue();
    [Fact] void should_have_shared_property_on_event_b() => _result.From[(EventType)"EventB"].Properties.ContainsKey(new PropertyPath("sharedProperty")).ShouldBeTrue();
    [Fact] void should_have_shared_property_on_event_c() => _result.From[(EventType)"EventC"].Properties.ContainsKey(new PropertyPath("sharedProperty")).ShouldBeTrue();
    [Fact] void should_map_shared_property_correctly() => _result.From[(EventType)"EventA"].Properties[new PropertyPath("sharedProperty")].ShouldEqual("sharedValue");
}

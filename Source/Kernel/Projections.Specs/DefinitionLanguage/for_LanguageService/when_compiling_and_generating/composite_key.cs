// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class composite_key : given.a_language_service_with_schemas<given.OrderReadModel>
{
    const string Definition = """
        projection Order => OrderReadModel
          from OrderCreated
            key OrderKey
              customerId = customerId
              orderNumber = orderNumber
            total = total
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.OrderCreated)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_from_order_created() => _result.From.ContainsKey((EventType)"OrderCreated").ShouldBeTrue();
    [Fact] void should_have_composite_key() => _result.From[(EventType)"OrderCreated"].Key.Value.ShouldContain(WellKnownExpressions.Composite);
    [Fact] void should_have_total_property() => _result.From[(EventType)"OrderCreated"].Properties.ContainsKey(new PropertyPath("total")).ShouldBeTrue();
    [Fact] void should_map_total_to_event_total() => _result.From[(EventType)"OrderCreated"].Properties[new PropertyPath("total")].ShouldEqual("total");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class composite_key : given.a_language_service
{
    const string Definition = """
        projection Order => OrderReadModel
          from OrderCreated
            key OrderKey {
              CustomerId = customerId
              OrderNumber = orderNumber
            }
            Total = total
        """;

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition, "OrderReadModel");

    [Fact] void should_have_from_order_created() => _result.From.ContainsKey((EventType)"OrderCreated").ShouldBeTrue();
    [Fact] void should_have_composite_key() => _result.From[(EventType)"OrderCreated"].Key.Value.ShouldContain("$composite");
    [Fact] void should_have_total_property() => _result.From[(EventType)"OrderCreated"].Properties.ContainsKey(new PropertyPath("Total")).ShouldBeTrue();
    [Fact] void should_map_total_to_event_total() => _result.From[(EventType)"OrderCreated"].Properties[new PropertyPath("Total")].ShouldEqual("total");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class complex_projection_with_shared_properties_and_add_operations : given.a_language_service_with_schemas<given.SimulatedOrderReadModel>
{
    const string Declaration = """
        projection SimulatedOrderProjection => SimulatedOrderReadModel
            from OrderPlaced key orderId
            from ItemLoadedOnTransport
                add totalDistance by distance
                add totalElapsedTime by elapsedTime
                add totalCo2FootPrint by co2FootPrint
                add totalCost by cost
            from ItemDeliveredToDestination
                add totalDistance by distance
                add totalElapsedTime by elapsedTime
                add totalCo2FootPrint by co2FootPrint
                add totalCost by cost
        """;

    protected override IEnumerable<Type> EventTypes => [
        typeof(given.OrderPlaced),
        typeof(given.ItemLoadedOnTransport),
        typeof(given.ItemDeliveredToDestination)];

    given.CompilerResult _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_three_from_definitions() => _result.Definition.From.Count.ShouldEqual(3);
    [Fact] void should_have_from_order_placed() => _result.Definition.From.ContainsKey((EventType)"OrderPlaced").ShouldBeTrue();
    [Fact] void should_have_from_item_loaded_on_transport() => _result.Definition.From.ContainsKey((EventType)"ItemLoadedOnTransport").ShouldBeTrue();
    [Fact] void should_have_from_item_delivered_to_destination() => _result.Definition.From.ContainsKey((EventType)"ItemDeliveredToDestination").ShouldBeTrue();

    [Fact] void should_not_have_explicit_mappings_in_order_placed() => _result.Definition.From[(EventType)"OrderPlaced"].Properties.Count.ShouldEqual(0);

    [Fact] void should_have_four_add_operations_in_item_loaded() => _result.Definition.From[(EventType)"ItemLoadedOnTransport"].Properties.Count.ShouldEqual(4);
    [Fact] void should_add_total_distance_in_item_loaded() => _result.Definition.From[(EventType)"ItemLoadedOnTransport"].Properties[new PropertyPath("totalDistance")].ShouldEqual($"{WellKnownExpressions.Add}(distance)");
    [Fact] void should_add_total_elapsed_time_in_item_loaded() => _result.Definition.From[(EventType)"ItemLoadedOnTransport"].Properties[new PropertyPath("totalElapsedTime")].ShouldEqual($"{WellKnownExpressions.Add}(elapsedTime)");
    [Fact] void should_add_total_co2_foot_print_in_item_loaded() => _result.Definition.From[(EventType)"ItemLoadedOnTransport"].Properties[new PropertyPath("totalCo2FootPrint")].ShouldEqual($"{WellKnownExpressions.Add}(co2FootPrint)");
    [Fact] void should_add_total_cost_in_item_loaded() => _result.Definition.From[(EventType)"ItemLoadedOnTransport"].Properties[new PropertyPath("totalCost")].ShouldEqual($"{WellKnownExpressions.Add}(cost)");

    [Fact] void should_have_four_add_operations_in_item_delivered() => _result.Definition.From[(EventType)"ItemDeliveredToDestination"].Properties.Count.ShouldEqual(4);

    [Fact] void should_not_generate_order_id_mapping_in_order_placed() => _result.GeneratedDefinition.ShouldNotContain("orderId = orderId");
    [Fact] void should_not_generate_destination_postal_code_mapping_in_order_placed() => _result.GeneratedDefinition.ShouldNotContain("destinationPostalCode = destinationPostalCode");
    [Fact] void should_not_generate_order_date_mapping_in_order_placed() => _result.GeneratedDefinition.ShouldNotContain("orderDate = orderDate");
    [Fact] void should_not_generate_optimal_route_mapping_in_order_placed() => _result.GeneratedDefinition.ShouldNotContain("optimalRoute = optimalRoute");

    [Fact] void should_not_generate_order_id_mapping_in_item_loaded() => _result.GeneratedDefinition.ShouldNotContain("orderId = orderId");
    [Fact] void should_not_generate_simulation_id_mapping_in_item_loaded() => _result.GeneratedDefinition.ShouldNotContain("simulationId = simulationId");
    [Fact] void should_not_generate_simulation_configuration_id_mapping_in_item_loaded() => _result.GeneratedDefinition.ShouldNotContain("simulationConfigurationId = simulationConfigurationId");
    [Fact] void should_not_generate_simulation_run_id_mapping_in_item_loaded() => _result.GeneratedDefinition.ShouldNotContain("simulationRunId = simulationRunId");
    [Fact] void should_not_generate_timestamp_mapping_in_item_loaded() => _result.GeneratedDefinition.ShouldNotContain("timestamp = timestamp");

    [Fact] void should_not_generate_simulation_id_mapping_in_item_delivered() => _result.GeneratedDefinition.ShouldNotContain("simulationId = simulationId");
    [Fact] void should_not_generate_simulation_configuration_id_mapping_in_item_delivered() => _result.GeneratedDefinition.ShouldNotContain("simulationConfigurationId = simulationConfigurationId");
    [Fact] void should_not_generate_simulation_run_id_mapping_in_item_delivered() => _result.GeneratedDefinition.ShouldNotContain("simulationRunId = simulationRunId");
    [Fact] void should_not_generate_destination_postal_code_mapping_in_item_delivered() => _result.GeneratedDefinition.ShouldNotContain("destinationPostalCode = destinationPostalCode");
    [Fact] void should_not_generate_timestamp_mapping_in_item_delivered() => _result.GeneratedDefinition.ShouldNotContain("timestamp = timestamp");

    [Fact] void should_generate_add_total_distance() => _result.GeneratedDefinition.ShouldContain("add totalDistance by distance");
    [Fact] void should_generate_add_total_elapsed_time() => _result.GeneratedDefinition.ShouldContain("add totalElapsedTime by elapsedTime");
    [Fact] void should_generate_add_total_co2_foot_print() => _result.GeneratedDefinition.ShouldContain("add totalCo2FootPrint by co2FootPrint");
    [Fact] void should_generate_add_total_cost() => _result.GeneratedDefinition.ShouldContain("add totalCost by cost");
}

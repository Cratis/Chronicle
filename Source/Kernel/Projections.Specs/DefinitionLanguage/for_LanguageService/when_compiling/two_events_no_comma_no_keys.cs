// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.given;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class two_events_no_comma_no_keys : a_language_service
{
    const string Definition = """
        projection TransportRoute => TransportRouteReadModel
          automap
          from HubRouteAdded
          from WarehouseRouteAdded
        """;

    ProjectionDefinition _result;

    void Because()
    {
        var result = _languageService.Compile(
            Definition,
            _projectionId,
            Concepts.Projections.ProjectionOwner.Client,
            Concepts.EventSequences.EventSequenceId.Log);
        _result = result.Match(
            projectionDef => projectionDef,
            errors => throw new InvalidOperationException($"Compilation failed: {string.Join(", ", errors.Errors)}"));
    }

    [Fact] void should_have_from_hub_route_added() => _result.From.ContainsKey((EventType)"HubRouteAdded").ShouldBeTrue();
    [Fact] void should_have_from_warehouse_route_added() => _result.From.ContainsKey((EventType)"WarehouseRouteAdded").ShouldBeTrue();
}

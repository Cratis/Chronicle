// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_multiple_flat_definitions_are_refused : given.a_projections_manager_grain
{
    ProjectionDefinition[] _definitions;
    Exception _error;

    void Establish()
    {
        _definitions = [FlatDefinition("first"), FlatDefinition("second")];
        _readModelDefinitions = [CreateReadModelDefinition("read-model")];
        _eventStoreStorage.GetNamespace(EventStoreNamespaceName.Default).Sinks.SupportsFlatCompositeKeys(Arg.Any<ReadModelDefinition>()).Returns(false);
        _readModelsManager.ClearReceivedCalls();
    }

    async Task Because() => _error = await Catch.Exception(() => _grain.Register(_definitions));

    [Fact] void should_report_the_rejections() => _error.ShouldNotBeNull();
    [Fact] async Task should_fetch_read_models_once_for_the_whole_preflight() => await _readModelsManager.Received(1).GetDefinitions();

    static ProjectionDefinition FlatDefinition(ProjectionId identifier) => CreateDefinition(identifier, "read-model") with
    {
        From = new Dictionary<EventType, FromDefinition>
        {
            [new EventType("Created", EventTypeGeneration.First)] = new(new Dictionary<PropertyPath, string>(), "$flatComposite(customer=$eventSourceId,number=number)", null)
        }
    };
}

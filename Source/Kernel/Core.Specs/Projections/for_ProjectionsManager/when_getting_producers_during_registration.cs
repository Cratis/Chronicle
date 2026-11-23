// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager;

public class when_getting_producers_during_registration : given.a_projections_manager_grain
{
    TaskCompletionSource<IEnumerable<EventStoreNamespaceName>> _namespacesReady;
    ProjectionDefinition _incoming;
    IEnumerable<ProjectionDefinition> _result;

    void Establish()
    {
        _incoming = CreateDefinition("in-flight", "read-model") with { EventSequenceId = "system" };
        _readModelDefinitions = [CreateReadModelDefinition(_incoming.ReadModel)];
        _namespacesReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([]), _namespacesReady.Task);
        _eventStoreStorage.Projections.GetProducersFor(Arg.Any<ReadModelIdentifier>(), Arg.Any<ProjectionId?>()).Returns([]);
    }

    async Task Because()
    {
        var registration = _grain.Register([_incoming]);
        try
        {
            while (_namespaces.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(_namespaces.GetAll)) < 2) await Task.Yield();
            _result = await _grain.GetProducersFor("read-model", null);
        }
        finally
        {
            _namespacesReady.SetResult([]);
            await registration;
        }
    }

    [Fact] void should_include_the_in_flight_system_producer() => _result.ShouldContain(_incoming);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.for_ProjectionsManager.given;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReadPolicy.given;

public class a_read_policy : Specification
{
    protected IGrainFactory _grains;
    protected IProjectionsManager _manager;
    protected IReducersManager _reducers;
    protected ReadModelDefinition _definition;
    protected ProjectionDefinition _supported;
    protected ProjectionDefinition _unsupported;
    protected Exception? _error;

    void Establish()
    {
        _grains = Substitute.For<IGrainFactory>();
        _manager = Substitute.For<IProjectionsManager>();
        Producers();
        _grains.GetGrain<IProjectionsManager>(Arg.Any<string>()).Returns(_manager);
        _reducers = Substitute.For<IReducersManager>();
        _grains.GetGrain<IReducersManager>(Arg.Any<string>()).Returns(_reducers);
        _definition = a_projections_manager_grain.CreateReadModelDefinition("read-model") with { ObserverIdentifier = string.Empty };
        _supported = a_projections_manager_grain.CreateDefinition("supported", _definition.Identifier);
        _unsupported = a_projections_manager_grain.CreateDefinition("unsupported", _definition.Identifier) with { EventSequenceId = "system" };
    }

    protected void Producers(params ProjectionDefinition[] definitions) =>
        _manager.GetProducersFor(Arg.Any<ReadModelIdentifier>(), Arg.Any<ProjectionId?>()).Returns(definitions);
}

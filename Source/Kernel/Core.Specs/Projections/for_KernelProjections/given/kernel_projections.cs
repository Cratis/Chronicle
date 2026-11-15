// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Statistics;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Projections.for_KernelProjections.given;

public class kernel_projections : Specification
{
    protected const string EventStore = "the-event-store";
    protected IProjectionsManager _manager;
    protected IReadModelsManager _readModels;
    protected KernelProjections _kernel;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(EventTypeStatisticsProjection)]);
        var factory = Substitute.For<IGrainFactory>();
        _manager = Substitute.For<IProjectionsManager>();
        _readModels = Substitute.For<IReadModelsManager>();
        factory.GetGrain<IProjectionsManager>(EventStore).Returns(_manager);
        factory.GetGrain<IReadModelsManager>(EventStore).Returns(_readModels);
        _kernel = new KernelProjections(types, factory, Options.Create(new ChronicleOptions()));
    }
}

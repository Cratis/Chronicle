// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationMigrationReactor.given;

public class a_reactor : Specification
{
    protected EventTypeGenerationMigrationReactor _reactor;
    protected IGrainFactory _grainFactory;
    protected IJobsManager _jobsManager;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _jobsManager = Substitute.For<IJobsManager>();
        _grainFactory.GetGrain<IJobsManager>(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>()).Returns(_jobsManager);
        _reactor = new EventTypeGenerationMigrationReactor(_grainFactory, Substitute.For<ILogger<EventTypeGenerationMigrationReactor>>());
    }
}

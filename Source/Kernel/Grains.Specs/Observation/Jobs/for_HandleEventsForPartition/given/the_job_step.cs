// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Core;
using Orleans.TestKit;
namespace Cratis.Chronicle.Grains.Observation.Jobs.for_HandleEventsForPartition.given;

public class the_job_step : Specification
{
    protected HandleEventsForPartition _jobStep;
    protected IStorage<HandleEventsForPartitionState> _stateStorage;
    protected TestKitSilo _silo = new();
    protected JobStepId _jobStepId;
    protected IObserver _observer;

    async Task Establish()
    {
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
        
        _jobStepId = JobStepId.New();
        var logger = _silo.AddService(NullLogger<Observer>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        _stateStorage = _silo.StorageManager.GetStorage<HandleEventsForPartitionState>(typeof(Observer).FullName);
        _jobStep = await _silo.CreateGrainAsync<HandleEventsForPartition>(_jobStepId);
    }
}

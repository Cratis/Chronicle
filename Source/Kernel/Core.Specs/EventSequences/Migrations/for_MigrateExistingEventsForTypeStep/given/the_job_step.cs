// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.Migrations.for_MigrateExistingEventsForTypeStep.given;

public class the_job_step : Specification
{
    protected MigrateExistingEventsForTypeStep _jobStep;
    protected IPersistentState<MigrateExistingEventsForTypeStepState> _stateStorage;
    protected TestKitSilo _silo = new();
    protected JobStepId _jobStepId;
    protected JobStepKey _jobStepKey;
    protected IStorage _storage;
    protected IEventTypeMigrations _eventTypeMigrations;
    protected EventTypeId _eventTypeId;
    protected MigrateExistingEventsForTypeRequest _request;

    async Task Establish()
    {
        _eventTypeId = new EventTypeId(Guid.NewGuid().ToString());
        _request = new MigrateExistingEventsForTypeRequest(_eventTypeId);

        _storage = Substitute.For<IStorage>();
        _eventTypeMigrations = Substitute.For<IEventTypeMigrations>();

        _silo.AddService(_storage);
        _silo.AddService(_eventTypeMigrations);
        _silo.AddService(new JsonSerializerOptions());
        _silo.AddService(Substitute.For<IJobStepThrottle>());

        var logger = _silo.AddService(NullLogger<MigrateExistingEventsForTypeStep>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        _jobStepId = JobStepId.New();
        _jobStepKey = new JobStepKey(JobId.New(), "test-event-store", "default");

        _stateStorage = _silo.AddPersistentStateStorage<MigrateExistingEventsForTypeStepState>(
            nameof(MigrateExistingEventsForTypeStepState),
            WellKnownGrainStorageProviders.JobSteps);

        _jobStep = await _silo.CreateGrainAsync<MigrateExistingEventsForTypeStep>(_jobStepId, _jobStepKey);
    }
}

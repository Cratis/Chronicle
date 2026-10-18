// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_rebuild_cancelled_outside_the_worker : a_rebuilding_constraint_index
{
    protected ReindexConstraintsStep _step;
    protected Exception? _cancellationError;

    async Task Establish()
    {
        _eventStoreStorage.Constraints.GetDefinitions().Returns(_ => _registeredConstraints.ToArray());
        var stepSilo = new TestKitSilo();
        stepSilo.AddService(_storage);
        stepSilo.AddService(_complianceManager);
        stepSilo.AddService(_expandoObjectConverter);
        stepSilo.AddService(Substitute.For<IJobStepThrottle>());
        stepSilo.AddService(NullLogger<ReindexConstraintsStep>.Instance);
        stepSilo.AddProbe<IEventSequence>(_ => _eventSequence);
        stepSilo.AddPersistentStateStorage<ReindexConstraintsStepState>(nameof(ReindexConstraintsStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps);
        _step = await stepSilo.CreateGrainAsync<ReindexConstraintsStep>(_runId.Value, new JobStepKey(JobId.New(), EventStore, EventStoreNamespace));
        await _eventSequence.ConstraintIndexRebuildStarting(new(_runId.Value, Guid.NewGuid()), [_uniqueDefinition.Name]);
        await _claims.ClearValues(_uniqueDefinition);
    }

    protected async Task CancelOutsideWorker()
    {
        var state = new ReindexConstraintsStepState { EventSequenceId = SequenceId, Changes = [new(_uniqueDefinition.Name, true, [ConstraintChangeType.EventAdded])] };
        _cancellationError = await Catch.Exception(async () => await (ValueTask<object?>)typeof(ReindexConstraintsStep)
            .GetMethod("CreateCancelledResultFromCurrentState", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_step, [state])!);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_held_constraint_index_rebuild : a_rebuilding_constraint_index
{
    protected readonly JobId _jobId = JobId.New();
    protected readonly List<JobStepState> _steps = [];
    protected readonly HeldConstraintRebuildLogger _heldLogger = new();
    protected ConstraintIndexRebuildRunId _heldRunId;
    protected IJobStepStorage _jobSteps;
    protected JobStepStatus _stepStatus = JobStepStatus.Failed;

    protected override Task<EventSequence> CreateEventSequence()
    {
        _eventSequenceLogger = _heldLogger.Logger;
        return base.CreateEventSequence();
    }

    void Establish()
    {
        _heldRunId = new(Guid.NewGuid(), Guid.NewGuid(), _clock.GetUtcNow(), _jobId);
        _jobSteps = Substitute.For<IJobStepStorage>();
        _namespaceStorage.JobSteps.Returns(_jobSteps);
        _jobSteps.GetForJob(_jobId, Arg.Any<JobStepStatus[]>()).Returns(_ =>
            Catch<IImmutableList<JobStepState>>.Success(_steps.ToImmutableList()));
    }

    protected async Task HoldRebuild()
    {
        _steps.Add(new()
        {
            Id = new(_jobId, _heldRunId.Value),
            Status = _stepStatus,
            StatusChanges = [new() { Status = _stepStatus, Occurred = _clock.GetUtcNow() }]
        });
        await _eventSequence.ConstraintIndexRebuildStarting(_heldRunId, [_uniqueDefinition.Name]);
        await _claims.ClearValues(_uniqueDefinition);
    }

    protected async Task PassLivenessCheck()
    {
        _clock.Advance(EventSequence.ConstraintIndexRebuildLivenessInterval);
        await Tick();
    }

    protected async Task CompleteCatchUp()
    {
        await Tick();
        await Tick();
    }

    protected async Task<AppendResult> AppendValue(string value)
    {
        _expandoObjectConverter.ToExpandoObject(Arg.Any<System.Text.Json.Nodes.JsonObject>(), Arg.Any<Schemas.JsonSchema>()).Returns(_ => Content(value));
        return await AppendAnEvent();
    }
}

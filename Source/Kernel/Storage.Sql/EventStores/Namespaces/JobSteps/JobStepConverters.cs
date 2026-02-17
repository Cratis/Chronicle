// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.JobSteps;

/// <summary>
/// Converters for job steps.
/// </summary>
public static class JobStepConverters
{
    /// <summary>
    /// Convert from entity to job step state.
    /// </summary>
    /// <param name="entity">Entity to convert from.</param>
    /// <returns>Converted job step state.</returns>
    public static JobStepState ToJobStepState(this JobStep entity)
    {
        if (string.IsNullOrEmpty(entity.StateJson))
        {
            return new JobStepState
            {
                Id = new JobStepIdentifier((JobId)entity.JobId, (JobStepId)entity.JobStepId),
                Type = new JobStepType(entity.Type),
                Name = (JobStepName)entity.Name,
                Status = entity.Status,
                IsPrepared = entity.IsPrepared
            };
        }

        var jobStepState = JsonSerializer.Deserialize<JobStepState>(entity.StateJson) ?? new JobStepState();

        // Ensure the key properties are consistent with entity values
        jobStepState.Id = new JobStepIdentifier((JobId)entity.JobId, (JobStepId)entity.JobStepId);
        jobStepState.Type = new JobStepType(entity.Type);
        jobStepState.Name = (JobStepName)entity.Name;
        jobStepState.Status = entity.Status;
        jobStepState.IsPrepared = entity.IsPrepared;

        return jobStepState;
    }

    /// <summary>
    /// Convert from job step state to entity.
    /// </summary>
    /// <param name="jobStepState">Job step state to convert from.</param>
    /// <param name="entityId">Unique identifier for the entity.</param>
    /// <returns>Converted entity.</returns>
    public static JobStep ToEntity(this JobStepState jobStepState, Guid entityId) =>
        new()
        {
            Id = entityId,
            JobId = jobStepState.Id.JobId.Value,
            JobStepId = jobStepState.Id.JobStepId.Value,
            Type = jobStepState.Type.Value,
            Name = jobStepState.Name.Value,
            Status = jobStepState.Status,
            IsPrepared = jobStepState.IsPrepared,
            StateJson = JsonSerializer.Serialize(jobStepState)
        };
}

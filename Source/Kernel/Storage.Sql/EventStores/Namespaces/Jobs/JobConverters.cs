// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Jobs;

/// <summary>
/// Converters for jobs.
/// </summary>
public static class JobConverters
{
    /// <summary>
    /// Convert from entity to job state.
    /// </summary>
    /// <param name="entity">Entity to convert from.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for deserializing the polymorphic <c>JobState.Request</c>.</param>
    /// <returns>Converted job state.</returns>
    public static JobState ToJobState(this Job entity, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(entity.StateJson))
        {
            return new JobState
            {
                Id = (JobId)entity.Id,
                Type = new JobType(entity.Type),
                Status = entity.Status,
                Created = entity.Created
            };
        }

        var jobState = JsonSerializer.Deserialize<JobState>(entity.StateJson, options) ?? new JobState();

        // Ensure the key properties are consistent with entity values
        jobState.Id = (JobId)entity.Id;
        jobState.Type = new JobType(entity.Type);
        jobState.Status = entity.Status;
        jobState.Created = entity.Created;

        return jobState;
    }

    /// <summary>
    /// Convert from job state to entity.
    /// </summary>
    /// <param name="jobState">Job state to convert from.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serializing the polymorphic <c>JobState.Request</c>.</param>
    /// <returns>Converted entity.</returns>
    public static Job ToEntity(this JobState jobState, JsonSerializerOptions options) =>
        new()
        {
            Id = jobState.Id.Value,
            Type = jobState.Type.Value,
            Status = jobState.Status,
            Created = jobState.Created,
            StateJson = JsonSerializer.Serialize(jobState, options)
        };
}

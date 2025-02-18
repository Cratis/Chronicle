// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
namespace Cratis.Chronicle.Storage.Jobs;

/// <summary>
/// The types of errors related to jobs.
/// </summary>
#pragma warning disable CA1008 // No none
public enum JobError
#pragma warning restore CA1008
{
    /// <summary>
    /// The job was not found.
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// The <see cref="Type"/> of the job is derived from <see cref="JobState"/>.
    /// </summary>
    TypeIsNotAJobStateType = 2,

    /// <summary>
    /// The <see cref="Type"/> of the job does not have an associated <see cref="JobType"/>.
    /// </summary>
    TypeIsNotAssociatedWithAJobType = 3,
}
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Jobs;

/// <summary>
/// The types of errors related to job steps.
/// </summary>
#pragma warning disable CA1008 // No none
public enum JobStepError
#pragma warning restore CA1008
{
    /// <summary>
    /// The job was not found.
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// The <see cref="Type"/> of the job is derived from <see cref="JobStepState"/>.
    /// </summary>
    TypeIsNotAJobStepStateType = 2
}

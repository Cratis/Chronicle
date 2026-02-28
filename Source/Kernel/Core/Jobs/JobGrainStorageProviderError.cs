// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// The exception that gets thrown when there was an error performing an operation on the <see cref="JobGrainStorageProvider"/>.
/// </summary>
public class JobGrainStorageProviderError : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobGrainStorageProviderError"/> class.
    /// </summary>
    /// <param name="jobStateType">The type of the job state.</param>
    /// <param name="error">The <see cref="JobError"/>.</param>
    /// <param name="methodName">The method.</param>
    public JobGrainStorageProviderError(Type jobStateType, Storage.Jobs.JobError error, string methodName)
        : base($"Error while performing {methodName} with job state type {jobStateType} : {Enum.GetName(error)}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobGrainStorageProviderError"/> class.
    /// </summary>
    /// <param name="jobStateType">The type of the job state.</param>
    /// <param name="error">The <see cref="Exception"/>.</param>
    /// <param name="methodName">The method.</param>
    public JobGrainStorageProviderError(Type jobStateType, Exception error, string methodName)
        : base($"Error while performing {methodName} with job state type {jobStateType}", error)
    {
    }
}
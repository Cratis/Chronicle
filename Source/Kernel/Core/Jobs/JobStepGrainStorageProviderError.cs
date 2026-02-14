// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// The exception that gets thrown when there was an error performing an operation on the <see cref="JobStepGrainStorageProvider"/>.
/// </summary>
public class JobStepGrainStorageProviderError : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobStepGrainStorageProviderError"/> class.
    /// </summary>
    /// <param name="jobStepStateType">The type of the job step state.</param>
    /// <param name="error">The <see cref="JobStepError"/>.</param>
    /// <param name="methodName">The method.</param>
    public JobStepGrainStorageProviderError(Type jobStepStateType, Storage.Jobs.JobStepError error, string methodName)
        : base($"Error while performing {methodName} with job step state type {jobStepStateType} : {Enum.GetName(error)}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStepGrainStorageProviderError"/> class.
    /// </summary>
    /// <param name="jobStepStateType">The type of the job step state.</param>
    /// <param name="error">The <see cref="Exception"/>.</param>
    /// <param name="methodName">The method.</param>
    public JobStepGrainStorageProviderError(Type jobStepStateType, Exception error, string methodName)
        : base($"Error while performing {methodName} with job step state type {jobStepStateType}", error)
    {
    }
}

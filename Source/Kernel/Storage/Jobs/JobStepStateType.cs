// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using OneOf.Types;

namespace Cratis.Chronicle.Storage.Jobs;

/// <summary>
/// Methods for verifying job step state type.
/// </summary>
public static class JobStepStateType
{
    /// <summary>
    /// Verify the job step state type.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns><see cref="Result{TResult,TError}"/> <see cref="None"/> and <see cref="JobStepError"/>.</returns>
    public static Result<None, JobStepError> Verify(Type type)
    {
        if (!typeof(JobStepState).IsAssignableFrom(type))
        {
            return JobStepError.TypeIsNotAJobStepStateType;
        }

        return default(None);
    }
}

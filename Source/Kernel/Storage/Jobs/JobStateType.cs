// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using OneOf.Types;

namespace Cratis.Chronicle.Storage.Jobs;

/// <summary>
/// Methods for verifying job state type.
/// </summary>
public static class JobStateType
{
    /// <summary>
    /// Verify the job state type.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns><see cref="Result{TResult,TError}"/> <see cref="None"/> and <see cref="JobError"/>.</returns>
    public static Result<None, JobError> Verify(Type type)
    {
        if (!typeof(JobState).IsAssignableFrom(type))
        {
            return JobError.TypeIsNotAJobStateType;
        }

        return default(None);
    }
}

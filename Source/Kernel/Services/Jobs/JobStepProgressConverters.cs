// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Services.Jobs;

/// <summary>
/// Extension methods for converting between <see cref="JobStepState"/> and <see cref="JobStep"/>.
/// </summary>
internal static class JobStepProgressConverters
{
    /// <summary>
    /// Convert from <see cref="JobStepState"/> to <see cref="JobStepProgress"/>.
    /// </summary>
    /// <param name="jobStepProgress"><see cref="JobStepState"/> to convert from.</param>
    /// <returns>Converted <see cref="JobStepProgress"/>.</returns>
    public static JobStepProgress ToContract(this Concepts.Jobs.JobStepProgress jobStepProgress) =>
        new()
        {
            Percentage = jobStepProgress.Percentage,
            Message = jobStepProgress.Message,
        };
}

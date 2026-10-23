// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Validates cluster roles and job worker configuration at startup.
/// </summary>
public class ClusterRolesValidator : IValidateOptions<ChronicleOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ChronicleOptions options)
    {
        var failures = new List<string>();
        var roles = options.Clustering.Roles;
        if (!roles.EventSequences && !roles.Observers && !roles.Jobs)
        {
            failures.Add("Clustering.Roles must enable at least one of EventSequences, Observers or Jobs.");
        }

        if (options.Clustering.JobsFallback != JobsFallback.AnySilo)
        {
            failures.Add("Clustering.JobsFallback must be AnySilo. Wait requires durable job waiting support (Cratis/Orleans#129).");
        }

        if (options.Jobs.MaxParallelStepsWithoutJobsRole is <= 0)
        {
            failures.Add("Jobs.MaxParallelStepsWithoutJobsRole must be greater than zero.");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}

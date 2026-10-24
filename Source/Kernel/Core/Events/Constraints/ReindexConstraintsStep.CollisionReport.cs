// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

public partial class ReindexConstraintsStep
{
    async Task<bool> ReportCollisions(Dictionary<ConstraintName, long> counts, CancellationToken cancellationToken)
    {
        async Task Report()
        {
            State.CollisionCounts = new Dictionary<ConstraintName, long>(counts);
            if (counts.Count > 0)
            {
                var constraints = counts.OrderBy(entry => entry.Key.Value, StringComparer.Ordinal)
                    .Select(entry => $"{entry.Key}={entry.Value}");
                State.Progress.Message = $"Skipped {counts.Values.Sum()} duplicate claims: {string.Join(", ", constraints)}";
            }

            var written = await WriteStateAsync();
            if (written.TryGetException(out var error)) throw error;
        }

        var reported = await TryInvokeOnActivation(Report, cancellationToken);
        return reported.IsSuccess;
    }
}

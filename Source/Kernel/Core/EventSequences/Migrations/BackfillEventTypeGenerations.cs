// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Represents an add-only backfill job with a distinct grain identity for rolling upgrades.
/// </summary>
public class BackfillEventTypeGenerations : Job<BackfillEventTypeGenerationsRequest, BackfillEventTypeGenerationsState>, IBackfillEventTypeGenerations
{
    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(BackfillEventTypeGenerationsRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(ImmutableList.Create(CreateStep<IBackfillEventTypeGenerationsStep>(request)));

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"Backfill generations for type {Request.EventTypeId}";

    /// <inheritdoc/>
    protected override Task<bool> CanResume() => Task.FromResult(true);
}

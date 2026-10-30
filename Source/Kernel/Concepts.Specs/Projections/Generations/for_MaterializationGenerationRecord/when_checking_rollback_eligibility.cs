// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.for_MaterializationGenerationRecord;

public class when_checking_rollback_eligibility : Specification
{
    [Theory]
    [InlineData(MaterializationGenerationState.Retired, 1, true)]
    [InlineData(MaterializationGenerationState.Retired, 0, false)]
    [InlineData(MaterializationGenerationState.Retired, -1, false)]
    [InlineData(MaterializationGenerationState.Retired, null, false)]
    [InlineData(MaterializationGenerationState.Building, 1, false)]
    [InlineData(MaterializationGenerationState.Following, 1, false)]
    [InlineData(MaterializationGenerationState.Active, 1, false)]
    [InlineData(MaterializationGenerationState.Archived, 1, false)]
    [InlineData(MaterializationGenerationState.Abandoned, 1, false)]
    [InlineData(MaterializationGenerationState.Removed, 1, false)]
    public void should_require_retired_state_and_an_unexpired_deadline(MaterializationGenerationState state, int? secondsUntilDeadline, bool expected)
    {
        var now = new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
        var identity = MaterializationGenerationIdentity.For("projection", MaterializationGeneration.First, "read-model");
        var record = new MaterializationGenerationRecord(identity, state, MaterializationRevision.Unknown, now)
        {
            RollbackDeadline = secondsUntilDeadline.HasValue ? now.AddSeconds(secondsUntilDeadline.Value) : null
        };

        record.IsRollbackEligible(now).ShouldEqual(expected);
    }
}

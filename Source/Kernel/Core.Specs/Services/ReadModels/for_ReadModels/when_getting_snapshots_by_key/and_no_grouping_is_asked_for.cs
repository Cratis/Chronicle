// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_snapshots_by_key;

/// <summary>
/// A caller that predates the grouping - the Time Machine, and every client built before there was a
/// choice - leaves the field unset, and must keep getting correlations.
/// </summary>
public class and_no_grouping_is_asked_for : given.a_projection_with_a_history
{
    GetSnapshotsByKeyResponse _result;

    async Task Because() => _result = await _service.GetSnapshotsByKey(new GetSnapshotsByKeyRequest
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = "test-read-model",
        EventSequenceId = "event-log",
        ReadModelKey = "my-instance"
    });

    [Fact] void should_group_by_correlation() => _result.Snapshots.Count.ShouldEqual(2);
}

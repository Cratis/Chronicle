// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed_with_failures;

/// <summary>
/// A retry made while the client is away fails, and the failed partition already records that attempt. Kept, the
/// job and its failed step piled up by the thousand within the retention period - and every observer subscribe and
/// unsubscribe loaded them all.
/// </summary>
public class and_the_partition_is_still_failed : given.a_retry_failed_partition_job
{
    [Fact] void should_not_keep_the_job() => _job.IsKeptAfterCompletedWithFailures.ShouldBeFalse();
}

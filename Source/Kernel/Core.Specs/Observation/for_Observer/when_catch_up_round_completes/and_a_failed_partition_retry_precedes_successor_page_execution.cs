// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_a_failed_partition_retry_precedes_successor_page_execution : and_a_failed_partition_retry_precedes_successor_preparation
{
    protected override bool DeliversPartitionPages => true;
}

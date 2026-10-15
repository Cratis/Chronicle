// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceQueue.when_waking;

public class with_durable_work_after_reactivation : given.a_queue_grain
{
    Occurrence _claim;
    async Task Establish() => await _queue.Record("durable", "{\"value\":1}", OccurrenceKind.Application);
    async Task Because()
    {
        await _grain.Wake();
        _claim = JsonSerializer.Deserialize<Occurrence[]>(await _grain.Claim("worker", 0, 1))!.Single();
    }

    [Fact] void should_claim_storage_work_without_an_ingress_call_on_this_activation() => _claim.Attempts.ShouldEqual(1);
    [Fact] void should_use_kernel_time() => _claim.LeaseExpiry.ShouldEqual(_now.AddSeconds(60));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_webhook_probe_with_a_receiver_deferral : a_webhook_ready_to_probe
{
    protected DateTimeOffset _deadline;
    protected PartitionDeferral _deferral;

    void Establish()
    {
        _deadline = _now + TimeSpan.FromHours(3);
        _deferral = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, _now, _deadline, 42UL);
        _failedPartitionsState.Partitions.Single().Deferral = _deferral;
    }
}

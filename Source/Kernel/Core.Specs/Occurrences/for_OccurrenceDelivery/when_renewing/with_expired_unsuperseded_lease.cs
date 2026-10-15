// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceDelivery.when_renewing;

public class with_expired_unsuperseded_lease : given.a_queue
{
    Occurrence _claim;
    OccurrenceLeaseResult _result;
    async Task Establish()
    {
        await _queue.Record("renew", "{\"value\":1}", OccurrenceKind.Application);
        _claim = (await _queue.Claim("worker", 1, 1)).Single();
        _clock.Now = _now.AddHours(1);
    }

    async Task Because() => _result = await _queue.Renew(new(_claim.Id, _claim.Epoch), 9999);
    [Fact] void should_accept_the_current_epoch() => _result.ShouldEqual(OccurrenceLeaseResult.Updated);
    [Fact] async Task should_cap_the_lease_at_ten_minutes_from_kernel_now() => (await _storage.Get(_claim.Id))!.LeaseExpiry.ShouldEqual(_now.AddHours(1).AddMinutes(10));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceQueue.when_receiving_a_reminder;

public class with_durable_work : given.a_queue_grain
{
    OccurrenceId _id;
    async Task Establish() => _id = new((await _queue.Record("reminder", "{\"value\":1}", OccurrenceKind.Application)).Id);
    async Task Because() => await _grain.ReceiveReminder("occurrence-wake", default);
    [Fact] async Task should_not_claim_work_on_behalf_of_a_worker() => (await _storage.Get(_id))!.State.ShouldEqual(OccurrenceState.Pending);
    [Fact] async Task should_leave_the_attempt_counter_unchanged() => (await _storage.Get(_id))!.Attempts.ShouldEqual(0);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_OccurrenceSnapshot.when_reading_status;

public class with_a_winning_effect_receipt : given.a_durable_snapshot
{
    OccurrenceSnapshot _snapshot;
    async Task Because() => _snapshot = (await OccurrenceSnapshot.GetOccurrence(_storeName, _namespace.Value, _occurrence.Id.Value, _storage))!;
    [Fact] void should_expose_the_current_epoch() => _snapshot.Epoch.ShouldEqual(7);
    [Fact] void should_expose_the_current_owner() => _snapshot.Owner.ShouldEqual("worker");
    [Fact] void should_expose_the_attempt_count() => _snapshot.Attempts.ShouldEqual(2);
    [Fact] void should_expose_the_recorded_effect_response() => _snapshot.Receipts.Single().Receipt.Response.ShouldEqual("{\"answer\":42}");
    [Fact] void should_identify_the_effect_namespace() => _snapshot.Receipts.Single().Namespace.ShouldEqual(_namespace.Value);
    [Fact] void should_preserve_the_receipts_original_epoch() => _snapshot.Receipts.Single().Receipt.Epoch.ShouldEqual(6);
}

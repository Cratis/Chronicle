// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_OccurrenceSnapshot.when_reading_status;

public class with_equal_effect_identities_in_two_namespaces : given.an_application_snapshot
{
    OccurrenceSnapshot _snapshot;

    async Task Because() => _snapshot = (await OccurrenceSnapshot.GetOccurrence(_storeName, _namespace.Value, _occurrence.Id.Value, _storage, applicationScope: true))!;

    [Fact] void should_preserve_both_namespace_receipts() => _snapshot.Receipts.Select(entry => entry.Namespace).ShouldContainOnly("tenant", "other-tenant");
    [Fact] void should_keep_equal_effect_identities_distinct() => _snapshot.Receipts.Select(entry => entry.Receipt.Id).ShouldEqual(["receipt", "receipt"]);
    [Fact] void should_pair_the_first_response_with_its_namespace() => _snapshot.Receipts.Single(entry => entry.Namespace == "tenant").Receipt.Response.ShouldEqual("{\"answer\":42}");
    [Fact] void should_pair_the_other_response_with_its_namespace() => _snapshot.Receipts.Single(entry => entry.Namespace == "other-tenant").Receipt.Response.ShouldEqual("{\"answer\":99}");
    [Fact] void should_exclude_effects_of_other_occurrences() => _snapshot.Receipts.Select(entry => entry.Receipt.Id).ShouldNotContain("second-receipt");
}

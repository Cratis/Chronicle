// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceSnapshot.when_observing;

public class with_multiple_occurrences_and_namespaces : given.an_application_snapshot
{
    OccurrenceSnapshot[] _snapshots;

    async Task Because() => _snapshots = (await OccurrenceSnapshot.ObserveOccurrences(_storeName, _namespace.Value, _occurrence.Trigger.Value, _storage, applicationScope: true).FirstAsync().ToTask().WaitAsync(TimeSpan.FromSeconds(10))).ToArray();

    [Fact] void should_emit_all_occurrences_in_view() => _snapshots.Select(snapshot => snapshot.Id).ShouldContainOnly("occurrence", "second-occurrence");
    [Fact] void should_attach_equal_effects_to_their_namespaces() => _snapshots.Single(snapshot => snapshot.Id == "occurrence").Receipts.Select(entry => entry.Namespace).ShouldContainOnly("tenant", "other-tenant");
    [Fact] void should_attach_the_second_effect_only_to_the_second_occurrence() => _snapshots.Single(snapshot => snapshot.Id == "second-occurrence").Receipts.Single().Receipt.Id.ShouldEqual("second-receipt");
    [Fact] async Task should_query_the_first_namespace_once_for_all_occurrences() => await ((IEffectReceiptStorage)_sequence).Received(1).GetEffectReceiptsForOccurrences(Arg.Is<IEnumerable<OccurrenceId>>(ids => ids.Count() == 2 && ids.Contains(_occurrence.Id) && ids.Contains(_secondOccurrence.Id)));
    [Fact] async Task should_query_the_other_namespace_once_for_all_occurrences() => await ((IEffectReceiptStorage)_otherSequence).Received(1).GetEffectReceiptsForOccurrences(Arg.Is<IEnumerable<OccurrenceId>>(ids => ids.Count() == 2 && ids.Contains(_occurrence.Id) && ids.Contains(_secondOccurrence.Id)));
    [Fact] async Task should_not_issue_individual_receipt_queries() => await ((IEffectReceiptStorage)_sequence).DidNotReceive().GetEffectReceipts(Arg.Any<OccurrenceId>());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model;

public class and_all_events_are_subscribed_alongside_an_explicit_from : given.a_mixed_all_event_history
{
    IEnumerable<ReadModelSnapshot> _result;

    async Task Because() => _result = await AllSnapshots();

    [Fact] void should_include_both_correlations() => _result.Select(snapshot => snapshot.CorrelationId).ShouldEqual<IEnumerable<Guid>>([FirstCorrelation.Value, SecondCorrelation.Value]);
    [Fact] void should_include_the_unmapped_event_in_history() => _result.SelectMany(snapshot => snapshot.Events).Select(@event => @event.Context.SequenceNumber).ShouldEqual<IEnumerable<ulong>>([1, 3, 2]);
}

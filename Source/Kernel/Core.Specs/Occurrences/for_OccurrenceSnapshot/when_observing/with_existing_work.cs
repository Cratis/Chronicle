// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceSnapshot.when_observing;

public class with_existing_work : given.a_durable_snapshot
{
    IEnumerable<OccurrenceSnapshot> _snapshots;
    async Task Because() => _snapshots = await OccurrenceSnapshot.ObserveOccurrences(_storeName, _namespace.Value, _occurrence.Trigger.Value, _storage).FirstAsync().ToTask().WaitAsync(TimeSpan.FromSeconds(10));
    [Fact] void should_emit_durable_work_without_waiting_for_an_append() => _snapshots.Single().Id.ShouldEqual(_occurrence.Id.Value);
    [Fact] void should_include_the_winning_effect() => _snapshots.Single().Receipts.Single().Receipt.Response.ShouldEqual("{\"answer\":42}");
}

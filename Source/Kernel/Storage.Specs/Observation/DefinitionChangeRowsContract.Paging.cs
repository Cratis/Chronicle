// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

public abstract partial class DefinitionChangeRowsContract
{
    [Fact] public async Task should_return_no_cursor_for_empty_results()
    {
        var page = await OpenRows<ObserverContinuation>().ReadPage(_observer, null, null, 10);
        page.Rows.ShouldBeEmpty();
        page.Next.ShouldBeNull();
    }
    [Fact] public async Task should_return_no_cursor_for_a_short_page()
    {
        await Insert(Continuation());
        var page = await OpenRows<ObserverContinuation>().ReadPage(_observer, null, null, 10);
        page.Rows.Count.ShouldEqual(1);
        page.Next.ShouldBeNull();
    }
    [Fact] public async Task should_reject_zero_page_limits() => (await Catch.Exception(async () => await OpenRows<ObserverContinuation>().ReadPage(_observer, null, null, 0))).ShouldBeOfExactType<InvalidObserverDefinitionChangePageLimit>();
    [Fact] public async Task should_reject_negative_page_limits() => (await Catch.Exception(async () => await OpenRows<ObserverContinuation>().ReadPage(_observer, null, null, -1))).ShouldBeOfExactType<InvalidObserverDefinitionChangePageLimit>();
    [Fact] public async Task should_cap_each_page_at_one_thousand_rows()
    {
        var row = Continuation();
        for (var index = 0; index < 1001; index++)
        {
            await Insert(row with { Key = row.Key with { Id = index.ToString(System.Globalization.CultureInfo.InvariantCulture) } });
        }
        var rows = OpenRows<ObserverContinuation>();
        var first = await rows.ReadPage(_observer, null, null, int.MaxValue);
        first.Rows.Count.ShouldEqual(1000);
        first.Next.ShouldNotBeNull();
        var last = await rows.ReadPage(_observer, null, first.Next, int.MaxValue);
        last.Rows.Count.ShouldEqual(1);
        last.Next.ShouldBeNull();
    }
    [Fact] public async Task should_page_after_a_deleted_cursor_row()
    {
        var row = Continuation();
        await Insert(row);
        await Insert(row with { Key = row.Key with { Id = "second" } });
        var rows = OpenRows<ObserverContinuation>();
        var first = await rows.ReadPage(_observer, new(1), null, 1);
        (await rows.TryDelete(first.Rows.Single().Key, 0)).ShouldBeTrue();
        var second = await rows.ReadPage(_observer, new(1), first.Next, 1);
        second.Rows.Count.ShouldEqual(1);
        second.Rows.Single().Key.ShouldNotEqual(first.Rows.Single().Key);
        var end = await rows.ReadPage(_observer, new(1), second.Next, 1);
        end.Rows.ShouldBeEmpty();
        end.Next.ShouldBeNull();
    }
    [Fact] public async Task should_isolate_every_observer_component()
    {
        var row = Continuation();
        await Insert(row);
        var others = new[]
        {
            _observer with { ObserverId = "other" }, _observer with { EventStore = "other" },
            _observer with { Namespace = "other" }, _observer with { EventSequenceId = "other" }
        };
        foreach (var observer in others) await Insert(row with { Key = row.Key with { Observer = observer } });
        (await OpenRows<ObserverContinuation>().ReadPage(_observer, null, null, 10)).Rows.Single().Key.ShouldEqual(row.Key);
    }
    [Fact] public async Task should_harvest_all_revisions_without_narrowing_to_one()
    {
        var row = Continuation();
        await Insert(row);
        await Insert(row with { Key = row.Key with { Revision = new(2) } });
        (await OpenRows<ObserverContinuation>().ReadPage(_observer, null, null, 10)).Rows.Count.ShouldEqual(2);
    }
    [Fact] public async Task should_narrow_to_the_requested_revision()
    {
        var row = Continuation();
        await Insert(row);
        await Insert(row with { Key = row.Key with { Revision = new(2) } });
        (await OpenRows<ObserverContinuation>().ReadPage(_observer, new(2), null, 10)).Rows.Single().Key.Revision.Value.ShouldEqual(2L);
    }
}

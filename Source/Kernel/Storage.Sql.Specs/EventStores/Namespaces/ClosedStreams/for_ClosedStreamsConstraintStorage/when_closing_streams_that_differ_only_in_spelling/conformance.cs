// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage.when_closing_streams_that_differ_only_in_spelling;

public abstract class conformance(given.ClosedStreamsHarness harness) : given.a_closed_streams_store(harness)
{
    const string Owner = "owner";
    const string Reopened = "order-1";

    readonly List<ClosedStream[]> _covering = [];
    readonly List<ClosedStream?> _found = [];
    ClosedStream[] _all;
    bool _reopened;
    ClosedStream[] _remaining;
    ClosedStream[] _coveringReopened;

    protected static ClosedStreamScope ScopeFor(string spelling) => new(EventSourceId: spelling, EventSourceType: spelling, EventStreamType: spelling, EventStreamId: spelling);

    async Task Because()
    {
        for (var index = 0; index < _spellings.Length; index++)
        {
            await Storage.Close(new(ScopeFor(_spellings[index]), Owner, (ulong)index + 1, null));
        }

        _all = [.. await Storage.GetAll()];
        var dimensions = (await Storage.GetDimensionsInUse()).ToArray();
        foreach (var spelling in _spellings)
        {
            _covering.Add([.. await Storage.GetCovering(ScopeFor(spelling), dimensions)]);
            _found.Add(await Storage.GetFor(Owner, ScopeFor(spelling)));
        }

        _reopened = await Storage.Reopen(Owner, ScopeFor(Reopened));
        _remaining = [.. await Storage.GetAll()];
        _coveringReopened = [.. await Storage.GetCovering(ScopeFor(Reopened), dimensions)];
    }

    [Fact] void should_store_each_spelling_as_its_own_closure() => _all.Length.ShouldEqual(_spellings.Length);
    [Fact] void should_read_back_every_event_source_id_exactly() => _all.Select(_ => _.Scope.EventSourceId!.Value).Order(StringComparer.Ordinal).ShouldEqual(_spellings.Order(StringComparer.Ordinal));
    [Fact] void should_read_back_every_event_source_type_exactly() => _all.Select(_ => _.Scope.EventSourceType!.Value).Order(StringComparer.Ordinal).ShouldEqual(_spellings.Order(StringComparer.Ordinal));
    [Fact] void should_read_back_every_stream_type_exactly() => _all.Select(_ => _.Scope.EventStreamType!.Value).Order(StringComparer.Ordinal).ShouldEqual(_spellings.Order(StringComparer.Ordinal));
    [Fact] void should_read_back_every_stream_id_exactly() => _all.Select(_ => _.Scope.EventStreamId!.Value).Order(StringComparer.Ordinal).ShouldEqual(_spellings.Order(StringComparer.Ordinal));

    [Fact]
    void should_cover_each_spelling_only_with_its_own_closure()
    {
        for (var index = 0; index < _spellings.Length; index++)
        {
            _covering[index].Select(_ => _.Scope.EventStreamId!.Value).ShouldEqual([_spellings[index]]);
        }
    }

    [Fact]
    void should_find_each_spelling_only_as_its_own_closure()
    {
        for (var index = 0; index < _spellings.Length; index++)
        {
            _found[index]!.SequenceNumber.Value.ShouldEqual((ulong)index + 1);
        }
    }

    [Fact] void should_reopen_the_spelling() => _reopened.ShouldBeTrue();
    [Fact] void should_keep_every_other_spelling_closed() => _remaining.Select(_ => _.Scope.EventStreamId!.Value).Order(StringComparer.Ordinal).ShouldEqual(_spellings.Where(_ => _ != Reopened).Order(StringComparer.Ordinal));
    [Fact] void should_no_longer_cover_the_reopened_spelling() => _coveringReopened.ShouldBeEmpty();
}
